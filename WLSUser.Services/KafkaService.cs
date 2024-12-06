using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WLS.KafkaMessenger;
using WLS.KafkaMessenger.Services.Interfaces;
using WLSUser.Domain.Models;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class KafkaService : IKafkaService
    {
        private readonly IKafkaMessengerService _kafkaMessengerService;
        private readonly ILogger<KafkaService> _logger;

        public KafkaService( IKafkaMessengerService kafkaMessengerService, ILogger<KafkaService> logger)
        {
            _kafkaMessengerService = kafkaMessengerService;
            _logger = logger;
        }

        public async Task<List<ReturnValue>> SendKafkaLoginMessage(UserModel user, UserMapping userMapping, DateTime date)
        {
            try
            {
                // Send Kafka message
                UserLogin loginObject = new UserLogin { 
                    Id = user.UserID, 
                    UniqueID = userMapping is null ? user.UniqueID : userMapping.CreateUniqueID(), 
                    Username = user.Username, LastLogin = date
                };

                return await _kafkaMessengerService.SendKafkaMessage(user.UserID.ToString(), "Login", loginObject, "wly.glb.pl.user");
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "SendKafkaLoginMessage - Error - {UniqueID}", userMapping.CreateUniqueID());
                return null;
            }
        }


        public async Task SendUserKafkaMessage(UserModel user, String subject)
        {
            try
            {
                UserModelKafka userModelKafka = TransformUserModelToKafka(user);
                var deliveryValues = await _kafkaMessengerService.SendKafkaMessage(userModelKafka.UserId.ToString(), subject, userModelKafka, "ck-phoenix-user");
                foreach(var deliveryValue in deliveryValues)
                {
                    if (deliveryValue.Status != 0) {
                      _logger.LogWarning(deliveryValue.Exception, "SendUserKafkaMessage - Delivery Error - {UniqueID}", user.UserID);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SendUserKafkaMessage - Unhandled exception, Error - {UniqueID}", user.UniqueID);
            }
        }

        private UserModelKafka TransformUserModelToKafka(UserModel user)
        {
            List<UserMappingKafka> userMappingsKafka = new List<UserMappingKafka>();
            foreach (UserMapping userMapping in user.UserMappings)
            {
                UserMappingKafka userMappingKafka = new UserMappingKafka
                {
                    Id = userMapping.Id,
                    UserId = userMapping.UserId,
                    PlatformName = userMapping.PlatformName,
                    PlatformCustomer = userMapping.PlatformCustomer,
                    PlatformRole = userMapping.PlatformRole,
                    PlatformUserId = userMapping.PlatformUserId,
                    PlatformAccountId = userMapping.PlatformAccountId,
                    PlatformData = userMapping.PlatformData,
                    Created = userMapping.Created,
                    CreatedBy = userMapping.CreatedBy,
                    Updated = userMapping.Updated,
                    UpdatedBy = userMapping.UpdatedBy
                };
                userMappingsKafka.Add(userMappingKafka);
            }

            UserModelKafka userModelKafka = new UserModelKafka
            {
                UserId = user.UserID,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Status = user.Status,
                LastUpdated = user.LastUpdated,
                Language = user.Language,
                Region = user.Region,
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy,
                UpdatedBy = user.UpdatedBy,
                UserMappings = userMappingsKafka
            };
            return userModelKafka;
        }
    }
}
