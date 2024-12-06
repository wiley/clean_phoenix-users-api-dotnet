using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using WLS.KafkaMessenger.Infrastructure;
using WLS.KafkaMessenger.Infrastructure.Interface;
using WLS.KafkaMessenger.Services;
using WLS.KafkaMessenger.Services.Interfaces;
using WLSUser.Domain.Models;
using WLSUser.Services;
using WLSUser.Services.Interfaces;
using Xunit;

namespace WLSUser.Tests.Services
{
    public class KafkaServiceTests
    {
        private readonly IKafkaService _kafkaService = null;
        private readonly ILogger<KafkaService> _logger;
        private readonly IKafkaConfig _kafkaConfig;
        private readonly IKafkaMessengerService _kafkaMessengerService;
        private readonly IProducer<string, string> _producer;
        private readonly string _host = "TestKafkaHost";
        private readonly string _topic = "TestKafkaTopic";

        public KafkaServiceTests()
        {
            _logger = Substitute.For<ILogger<KafkaService>>();

            _kafkaConfig = new KafkaConfig
            {
                Host = _host,
                Sender = new List<KafkaSender>
                {
                    new KafkaSender { Topic = _topic }
                }
            };
            _producer = Substitute.For<IProducer<string, string>>();
            _kafkaMessengerService = new KafkaMessengerService(_kafkaConfig, _producer);

            _kafkaService = new KafkaService(_kafkaMessengerService, _logger);
        }

        [Fact]
        public void SendKafkaLoginMessage_Success()
        {
            var deliveryResult = new DeliveryResult<string, string>
            {
                Offset = new Offset(1),
                Partition = new Partition(1),
                Topic = _topic,
                TopicPartitionOffset = new TopicPartitionOffset(
                new TopicPartition(_topic, new Partition(2)), new Offset(3)),
                Status = PersistenceStatus.Persisted
            };

            var user = new UserModel
            {
                UserID = 9000001,
                Username = "test"                
            };

            var userMapping = new UserMapping
            {
                Id = 1,
                UserId = 9000001,
                PlatformName = "epic",
                PlatformRole = "learner"
            };

            _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>()).ReturnsForAnyArgs(deliveryResult);

            var result = _kafkaService.SendKafkaLoginMessage(user, userMapping, DateTime.Now).Result;

            result[0].DeliveryResult.Should().BeEquivalentTo(deliveryResult);
        }

        [Fact]
        public void SendKafkaUserMessage_Success()
        {
            var deliveryResult = new DeliveryResult<string, string>
            {
                Offset = new Offset(1),
                Partition = new Partition(1),
                Topic = _topic,
                TopicPartitionOffset = new TopicPartitionOffset(
                new TopicPartition(_topic, new Partition(2)), new Offset(3)),
                Status = PersistenceStatus.Persisted
            };

            var user = new UserModel
            {
                UserID = 9000001,
                Username = "test"
            };

            _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>()).ReturnsForAnyArgs(deliveryResult);

            var result = _kafkaService.SendUserKafkaMessage(user, "UnitTest").Result;

            result[0].DeliveryResult.Should().BeEquivalentTo(deliveryResult);
        }
    }
}
