using Microsoft.Extensions.Configuration;
using RabbitMQ.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RabbitMQ.Service
{
    public class RabbitMQService
    {
        private readonly RabbitMQManager _rabbitmqManager;

        public RabbitMQService(IConfiguration configuration)
        {
            _rabbitmqManager = new RabbitMQManager(configuration);

           
            //初始化生产者连接
            _rabbitmqManager.InitProducerConnection();

            var queueConfigs = configuration.GetSection("MQ:Queues").Get<List<QueueConfigInfo>>();

            //初始化消费者连接
            _rabbitmqManager.InitConsumerConnections(queueConfigs);
        }

        public RabbitMQManager Instance => _rabbitmqManager;
    }
}
