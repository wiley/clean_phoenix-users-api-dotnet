using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WLS.KafkaMessenger;
using WLSUser.Domain.Models;

namespace WLSUser.Services.Interfaces
{
    public interface IKafkaService
    {
        Task<List<ReturnValue>> SendKafkaLoginMessage(UserModel user, UserMapping userMapping, DateTime date);
        Task SendUserKafkaMessage(UserModel user, String subject);
    }
}
