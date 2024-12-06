using Confluent.Kafka;
using DarwinAuthorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Registry;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using WLS.KafkaMessenger.Infrastructure;
using WLS.KafkaMessenger.Infrastructure.Interface;
using WLS.KafkaMessenger.Services;
using WLS.KafkaMessenger.Services.Interfaces;
using WLS.Log.LoggerTransactionPattern;
using WLS.Monitoring.HealthCheck;
using WLS.Monitoring.HealthCheck.Interfaces;
using WLSUser.Domain.Constants;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Domain.Models.Interfaces;
using WLSUser.Infrastructure.Contexts;
using WLSUser.Services;
using WLSUser.Services.Authentication;
using WLSUser.Services.Interfaces;
using WLSUser.Utils;
using Newtonsoft.Json;

namespace WLSUser
{
    public class Startup
    {
        private const string SecretKey = "a1Jf6hDkJ9SDcWy4JAwN2hOIi7LsXsf0"; // TODO: get this from somewhere secure
        private const string SecretExchangeKey = "yqLRK5axnVUn48u4kovWN6Lov6aBtAzY";
        private readonly SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(SecretKey));
        private readonly SymmetricSecurityKey _signingExchangeKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(SecretExchangeKey));
        private readonly ILogger<Startup> _logger;
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, ILogger<Startup> logger, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _logger = logger;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                options.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:sszzz";
            });

            var connectionString = Environment.GetEnvironmentVariable("USERSAPI_CONNECTION_STRING") ??
                Configuration.GetConnectionString("UserDbContext");

            var commandTimeout = Environment.GetEnvironmentVariable("USERSAPI_CONNECTION_COMMANDTIMEOUT") != null ?
                int.Parse(Environment.GetEnvironmentVariable("USERSAPI_CONNECTION_COMMANDTIMEOUT"))
                : 30;

            ConfigureCors(services);
            ConfigureJWT(services);
            ConfigureLogging(services);
            ConfigurePolicies(services);
            ConfigureOpenAPI(services);
            ConfigureHttpClients(services);
            ConfigureRedis(services);
            ConfigureVersioning(services);
            ConfigureDbContext(services, connectionString, commandTimeout);

            string privateKeyFile = Environment.GetEnvironmentVariable("PRIVATE_KEY_FILE");
            string privateKey = "";
            try
            {
                if (!string.IsNullOrEmpty(privateKeyFile) && File.Exists(privateKeyFile))
                    privateKey = File.ReadAllText(privateKeyFile);
                else
                    privateKey = File.ReadAllText("usersapi.pem");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "usersapi.pem file not found.  SSO will not be able to complete a login.");
            }

            services.AddSingleton<IAppConfig>(cfg => new AppConfig()
            {
                ConnectionString = connectionString,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                PrivateKey = privateKey
            });

            services.AddSingleton<IDbHealthCheck, DbHealthCheck>();
            services.AddSingleton<IHealthService, HealthService>();
            services.AddSingleton<IKafkaMessengerService, KafkaMessengerService>();

            string host = Environment.GetEnvironmentVariable("KAFKA_HOST");
            var senders = new List<KafkaSender>
            {
                new KafkaSender
                {
                    Topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC")
                }
            };
            services.AddSingleton<IKafkaConfig>(kc =>
                new KafkaConfig() { Host = host, Sender = senders, Source = "darwin-users" }
            );

            services.AddSingleton(p => new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = host
            }).Build());

            services.AddScoped<IKafkaService, KafkaService>();
            services.Add(new ServiceDescriptor(typeof(IUserService), typeof(UserService), ServiceLifetime.Transient));
            services.Add(new ServiceDescriptor(typeof(IUserMappingService), typeof(UserMappingService), ServiceLifetime.Transient));
            services.Add(new ServiceDescriptor(typeof(IUserConsentService), typeof(UserConsentService), ServiceLifetime.Transient));

            IConfigurationSection cookiesOptions = Configuration.GetSection("CookieOptions");
            services.Configure<CookiesOptions>(cookiesOptions);

            IConfigurationSection jwtExpirations = Configuration.GetSection("JwtExpirations");
            services.Configure<JwtExpirations>(jwtExpirations);

            //Add Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<IRedisService, RedisService>();
            services.AddScoped<ICookiesService, CookiesService>();
            services.AddScoped<IFederationService, FederationService>();
            services.AddScoped<ISsoService, SsoService>();
            services.AddScoped<JwtSecurityTokenHandler>();
            services.AddScoped<IKeyCloakService, KeyCloakService>();
            services.AddScoped<IJwtSessionService, JwtSessionService>();
            services.AddScoped<ILoggerStateFactory, LoggerStateFactory>();
        }

        private void ConfigureCors(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        // The token fingerprint and refresh token are passed as
                        // an http only cookie, the consuming client requests must
                        // be initiated with the "withCredentials" property
                        // in order to pass the http only cookie back, but the
                        // back end will only accept that cookie if CORS has been
                        // set up with specific origins. "AllowAnyOrigin" blocks "AllowCredentials".
                        // Using a "SetIsOriginAllowed" hack for dev/staging environments.
                        // https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS/Errors/CORSNotSupportingCredentials
                        builder
                        .SetIsOriginAllowed(origin => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                    });
            });
        }

        private void ConfigureJWT(IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddSingleton<IJwtFactory, JwtFactory>();

            // Get options from app settings
            var jwtAppSettingOptions = Configuration.GetSection(nameof(JwtIssuerOptions));

            // Configure JwtIssuerOptions
            services.Configure<JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            });

            var jwtExchangeSettingOptions = Configuration.GetSection(nameof(JwtExchangeOptions));

            services.Configure<JwtExchangeOptions>(options =>
            {
                options.Issuer = jwtExchangeSettingOptions[nameof(JwtExchangeOptions.Issuer)];
                options.Audience = jwtExchangeSettingOptions[nameof(JwtExchangeOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(_signingExchangeKey, SecurityAlgorithms.HmacSha256);
            });

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)],

                ValidateAudience = true,
                ValidAudience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)],

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,

                RequireExpirationTime = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
            services.AddDarwinAuthzConfiguration();
            services.AddAuthentication().AddJwtBearer("Custom", configureOptions => {
                configureOptions.ClaimsIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                configureOptions.TokenValidationParameters = tokenValidationParameters;
                configureOptions.SaveToken = true;
            
                if (Debugger.IsAttached || _env.IsDevelopment()) {
                    configureOptions.RequireHttpsMetadata = false;
                }
            }).AddScheme<JwtSchemeOptions, JwtCookieAuthenticationHandler>(AuthenticationSchemesConstants.JwtCookies, op => { });

            // api user claim policy
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiUser", policy => policy.RequireClaim(JwtClaimIdentifiers.Rol, JwtClaims.ApiAccess));
            });
        }
        
        private void ConfigureLogging(IServiceCollection services)
        {
            //Switching to using "Serilog" log provider for everything
            //
            // NOTE: Call to ClearProviders() is what turns off the default Console Logging
            //
            //Output to the Console is now controlled by the WriteTo format below
            //DEVOPS can control the Log output with environment variables
            //  LOG_MINIMUMLEVEL - values like INFORMATION, WARNING, ERROR
            //  LOG_JSON - true means to output log to console in JSON format

            LogLevel level = LogLevel.None;
            LoggingLevelSwitch serilogLevel = new LoggingLevelSwitch();
            serilogLevel.MinimumLevel = LogEventLevel.Information;

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LOG_MINIMUMLEVEL")))
            {
                Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("LOG_MINIMUMLEVEL"), out level);
                LogEventLevel eventLevel = LogEventLevel.Information;
                Enum.TryParse<LogEventLevel>(Environment.GetEnvironmentVariable("LOG_MINIMUMLEVEL"), out eventLevel);
                serilogLevel.MinimumLevel = eventLevel;
            }

            bool useJSON = (Environment.GetEnvironmentVariable("LOG_JSON") == "true");

            LoggerConfiguration config = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .ReadFrom.Configuration(Configuration)
                ;

            if (useJSON)
                config.WriteTo.Console(new ElasticsearchJsonFormatter());
            else
                config.WriteTo.Console(outputTemplate: "[{Timestamp:MM-dd-yyyy HH:mm:ss.SSS} {Level:u3}] {Message:lj} {TransactionID}{NewLine}{Exception}", theme: SystemConsoleTheme.Literate);

            if (level != LogLevel.None)
                config.MinimumLevel.ControlledBy(serilogLevel);

            Log.Logger = config.CreateLogger();

            services.AddLogging(lb =>
            {
                lb.ClearProviders();
                lb.AddSerilog();

                //This is the only way ("now") to add log content to the Debug Output window in Visual Studio
                lb.AddDebug(); //LogLevel/Default in appsettings can control this
            });
        }

        private void ConfigureDbContext(IServiceCollection services, string connectionString, int? commandTimeout)
        {
            services.AddDbContext<UserDbContext>(options =>
            {
                // skip mysql version detection when building efbundle
                string mysqlVersion=Environment.GetEnvironmentVariable("MYSQL_VERSION") ?? null;
                ServerVersion serverVersion=null;
                if (mysqlVersion != null) {
                    serverVersion=new MySqlServerVersion(mysqlVersion);
                } else {
                    serverVersion=ServerVersion.AutoDetect(connectionString);
                }
                options.UseMySql(
                    connectionString, 
                    serverVersion,
                    builder =>
                    {
                        builder.CommandTimeout(commandTimeout);
                    }
                    );
            }, ServiceLifetime.Transient);

            /*
            services.AddEntityFrameworkInMemoryDatabase();
            services.AddDbContext<UserDbContext>(options =>
            {
                options.UseInMemoryDatabase("WLSUser");
            }, ServiceLifetime.Singleton);
            */
        }

        private void ConfigurePolicies(IServiceCollection services)
        {
            IPolicyRegistry<string> policyRegistry = services.AddPolicyRegistry();
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500));
            policyRegistry.Add("timeout", timeoutPolicy);
        }

        private void ConfigureOpenAPI(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.OperationFilter<AddRequiredHeaderParameter>();

                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v1",
                    Title = "WLS Users API",
                    Description = "WLS Users API",
                });

                c.SwaggerDoc("v4", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v4",
                    Title = "WLS Users API",
                    Description = "WLS Users API",
                });

                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "UsersAPIToken",
                    In = ParameterLocation.Header,
                    Description = "An API Key is required."
                });

                c.AddSecurityDefinition("X-Api-Key", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "X-Api-Key",
                    In = ParameterLocation.Header,
                    Description = "An API Key is required."
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer <token>'",

                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                        },
                        new string[] { }
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] { }
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Api-Key" }
                        },
                        new string[] { }
                    },
                });
            });
        }

        private void ConfigureHttpClients(IServiceCollection services)
        {
            services.AddHttpClient<ILearnerEmailAPI, LearnerEmailAPIService>(options =>
                {
                    options.BaseAddress = new Uri(
                        Environment.GetEnvironmentVariable("LEARNER_EMAIL_API_BASE_URL") ??
                            Configuration["EPICLearnerEmail:BaseURL"]);
                    options.Timeout = TimeSpan.FromSeconds(30);
                })
                .AddPolicyHandlerFromRegistry("timeout")
                .AddTransientHttpErrorPolicy(p => p.RetryAsync(3))
                //.AddTypedClient(client => RestService.For<ILearnerEmailAPI>(client))
                ;
            services.AddHttpClient<IEmailAPIService, EmailAPIService>(options =>
            {
                options.BaseAddress = new Uri(Environment.GetEnvironmentVariable("EMAIL_API_BASE_URL"));
                options.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient("sso");
        }

        private void ConfigureRedis(IServiceCollection services)
        {
            IConfigurationSection redisOptions = Configuration.GetSection("RedisCache");
            string RedisServerConnection = Environment.GetEnvironmentVariable("REDIS_SERVER_CONNECTION");
            if (RedisServerConnection != null)
            {
                redisOptions["Connection"] = RedisServerConnection;
            };
            services.Configure<RedisServiceOptions>(redisOptions);
        }

        private void ConfigureVersioning(IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });
            services.AddVersionedApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";
                o.SubstituteApiVersionInUrl = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            //This approach was needed for an In Memory Database because it needed to be the same copy of the object
            //var userDbContext = app.ApplicationServices.GetService<UserDbContext>();
            //userDbContext.Initialize();

            //Because a MySql database context can be transient and initialization goes against the actualy database
            //and not a common in-memory object, then we can just create a new instance here:

            var connectionString = Environment.GetEnvironmentVariable("USERSAPI_CONNECTION_STRING") ??
                Configuration.GetConnectionString("UserDbContext");
            // Use regular expression to find the password part and replace it with empty string
            var cleanedConnectionString = Regex.Replace(connectionString, @"(?i)(Password)=([^;]+)", "");
            // Remove trailing semicolon
            cleanedConnectionString = cleanedConnectionString.TrimEnd(';');

            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            _logger.LogInformation("connectionString: {connectionString}", cleanedConnectionString);
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            var userDbContext = new UserDbContext(optionsBuilder.Options);
            userDbContext.Initialize();

            if (env.IsDevelopment() || env.IsStaging())
            {
                IdentityModelEventSource.ShowPII = true;
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(settings =>
                {
                    settings.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                    settings.SwaggerEndpoint("/swagger/v4/swagger.json", "v4");
                    settings.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                });

                app.UseCors("AllowAll");
            }
            else
            {
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseDarwinAuthenticationContext();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
