using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.V4;

namespace WLSUser.Infrastructure.Contexts
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<UserConsent> UserConsent { get; set; }
        public DbSet<UserMapping> UserMappings { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<RoleType> RoleTypes { get; set; }
        public DbSet<AccessType> AccessTypes { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserRoleAccess> UserRoleAccess { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }
        public DbSet<Federation> Federations { get; set; }
        public DbSet<SSOState> SSOStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users table
            modelBuilder.Entity<UserModel>()
                .HasKey(u => new { u.UserID });
            modelBuilder.Entity<UserModel>()
                .Property(u => u.UserID)
                .IsRequired()
                .ValueGeneratedOnAdd();
            //NOTE: We cannot set the Auto_Increment value in code because Pomelo does not yet support it: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1460#issuecomment-869083044
            //Solution: MySQL automatically updates the Seed based upon the last insert (so all we have to do is insert one record at Id=20,000,000 and it will generate the next one at 20,000,001
            //https://dev.mysql.com/doc/refman/5.6/en/example-auto-increment.html , https://dev.mysql.com/doc/refman/8.0/en/example-auto-increment.html - When you insert any other value into an AUTO_INCREMENT column, the column is set to that value and the sequence is reset so that the next automatically generated value follows sequentially from the largest column value.
            //MySQL Auto_Increment is cached: https://bugs.mysql.com/bug.php?id=91038 - To see the latest Auto_Increment value, you have to turn off a caching with "set session information_schema_stats_expiry=0;" - I saw this in development
            //MySQL doesn't support multiple auto increment columns - https://stackoverflow.com/questions/7344587/how-do-i-make-another-mysql-auto-increment-column, it appears it might with a myISAM table, but we have InnoDB tables
            modelBuilder.Entity<UserModel>()
                .HasIndex(u => new { u.Username, u.UserType });

            //UserConsent
            modelBuilder.Entity<UserConsent>()
                .HasKey(u => new { u.Id });
            modelBuilder.Entity<UserConsent>()
                .HasIndex(u => new { u.UserId });
            modelBuilder.Entity<UserConsent>()
                .HasOne(typeof(UserModel))
                .WithMany("UserConsents")
                .HasForeignKey("UserId")
                .HasPrincipalKey("UserID");

            // UserMapping table
            modelBuilder.Entity<UserMapping>()
                .HasKey(u => new { u.Id });
            modelBuilder.Entity<UserMapping>()
                .HasIndex(u => new { u.UserId});
            modelBuilder.Entity<UserMapping>()
                .HasOne(typeof(UserModel))
                .WithMany("UserMappings")
                .HasForeignKey("UserId")
                .HasPrincipalKey("UserID");

            // Brand table
            modelBuilder.Entity<Brand>()
                .HasKey(b => new { b.BrandID });
            modelBuilder.Entity<Brand>()
                .HasIndex(b => b.BrandID)
                .IsUnique();

            // RoleType table
            modelBuilder.Entity<RoleType>()
                .HasKey(rt => new { rt.RoleTypeID });
            modelBuilder.Entity<RoleType>()
                .HasIndex(rt => rt.RoleTypeID)
                .IsUnique();
            modelBuilder.Entity<RoleType>()
                .HasIndex(rt => rt.BrandID);

            // AccessType table
            modelBuilder.Entity<AccessType>()
                .HasKey(at => new { at.AccessTypeID });
            modelBuilder.Entity<AccessType>()
                .HasIndex(at => at.AccessTypeID)
                .IsUnique();

            // UserRole table
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserRoleID });
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.UserRoleID)
                .IsUnique();
            modelBuilder.Entity<UserRole>()
                .Property(ur => ur.UserRoleID)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.RoleTypeID);
            modelBuilder.Entity<UserRole>()
                .Property(ur => ur.Created)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            // UserRoleAccess table
            modelBuilder.Entity<UserRoleAccess>()
                .HasKey(ura => new { ura.UserRoleID, ura.AccessTypeID, ura.AccessRefID });
            modelBuilder.Entity<UserRoleAccess>()
                .HasIndex(ura => ura.UserRoleID);
            modelBuilder.Entity<UserRoleAccess>()
                .HasIndex(ura => ura.AccessTypeID);
            modelBuilder.Entity<UserRoleAccess>()
                .HasIndex(ura => ura.AccessRefID);
            modelBuilder.Entity<UserRoleAccess>()
                .HasIndex(ura => ura.GrantedBy);
            modelBuilder.Entity<UserRoleAccess>()
                .Property(ura => ura.Created)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            // LoginAttempts table
            modelBuilder.Entity<LoginAttempt>()
                .HasKey(la => new { la.LoginAttemptID });
            modelBuilder.Entity<LoginAttempt>()
                .HasIndex(la => la.LoginAttemptID);
            modelBuilder.Entity<LoginAttempt>()
                .Property(la => la.UserID)
                .IsRequired();
            modelBuilder.Entity<LoginAttempt>()
               .HasIndex(la => la.UserID);
            modelBuilder.Entity<LoginAttempt>()
                .Property(la => la.Attempted)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .IsRequired();
            modelBuilder.Entity<LoginAttempt>()
                .HasIndex(r => new { r.UserID, r.Attempted }).IsUnique().HasFilter(null);
            modelBuilder.Entity<LoginAttempt>()
                .Property(la => la.Success)
                .HasDefaultValueSql("0")
                .IsRequired();

            //Federation table
            modelBuilder.Entity<Federation>()
              .HasKey(f => f.Id);
            modelBuilder.Entity<Federation>()
              .Property(f => f.Id)
              .ValueGeneratedOnAdd();
            modelBuilder.Entity<Federation>()
              .HasIndex(f => f.Name)
              .IsUnique();
            modelBuilder.Entity<Federation>()
              .Property(la => la.Name)
              .IsRequired();
            modelBuilder.Entity<Federation>()
              .Property(la => la.OpenIdAuthInitUrl)
              .IsRequired();
            modelBuilder.Entity<Federation>()
              .Property(la => la.OpenIdClientId)
              .IsRequired();
            modelBuilder.Entity<Federation>()
              .Property(la => la.OpenIdClientSecret)
              .IsRequired();
            modelBuilder.Entity<Federation>()
              .Property(la => la.OpenIdTokenUrl)
              .IsRequired();
            modelBuilder.Entity<Federation>()
              .Property(la => la.RedirectUrl)
              .IsRequired();
            modelBuilder.Entity<Federation>()
                .Property(la => la.Scope)
                .IsRequired();
            modelBuilder.Entity<Federation>()
                .Property(la => la.AuthMethod)
                .IsRequired();

            //States table
            modelBuilder.Entity<SSOState>()
                .HasKey(f => f.Id);
            modelBuilder.Entity<SSOState>()
                .Property(f => f.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<SSOState>()
                .Property(f => f.Key)
                .IsRequired();
            modelBuilder.Entity<SSOState>()
                .HasIndex(f => f.Key)
                .IsUnique();
            modelBuilder.Entity<SSOState>()
                .Property(f => f.Created)
                .IsRequired();
        }

        public void Initialize()
        {
            //Database.Migrate();           
        }    

       
    }
}