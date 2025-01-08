using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using RabbitMQ.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace RabbitMQ.Service
{
    public class RabbitMQManager
    {
        //使用数组的部分，是给消费端用的，目前生产者只设置了一个，消费者可能存在多个。
        /// <summary>
        /// RabbitMQ工厂发送端
        /// </summary>
        private IConnectionFactory _connectionSendFactory;
        /// <summary>
        /// RabbitMQ工厂接收端
        /// </summary>
        private IConnectionFactory _connectionReceiveFactory;

        /// <summary>
        /// 连接 发送端
        /// </summary>
        private IConnection _connectionSend;
        /// <summary>
        /// 连接 消费端
        /// </summary>
        private IConnection[] _connectionReceive;

        /// <summary>
        /// MQ配置信息
        /// </summary>
        public MqConfigInfo _mqConfigs;

        /// <summary>
        /// 通道 发送端
        /// </summary>
        private IModel _modelSend;
        /// <summary>
        /// 通道 消费端
        /// </summary>
        private IModel[] _modelReceive;

        /// <summary>
        /// 事件
        /// </summary>
        private EventingBasicConsumer[] _basicConsumer;

        /// <summary>
        /// 消费者个数
        /// </summary>
        public int _costomerCount;
        public RabbitMQManager(IConfiguration configuration)
        {
            _mqConfigs = new MqConfigInfo
            {
                Host = configuration["MQ:Host"],
                Port = Convert.ToInt32(configuration["MQ:Port"]),
                User = configuration["MQ:User"],
                Password = configuration["MQ:Password"],
                ExchangeName = configuration["MQ:ExchangeName"],
                DeadLetterExchangeName = configuration["MQ:DeadLetterExchangeName"],
                DeadLetterQueueName = configuration["MQ:Queues:2:QueueName"]
            };
        }

        /// <summary>
        /// 初始化生产者连接
        /// </summary>
        public void InitProducerConnection()
        {
            Console.WriteLine("【开始】>>>>>>>>>>>>>>>生产者连接");

            _connectionSendFactory = new ConnectionFactory
            {
                HostName = _mqConfigs.Host,
                Port = _mqConfigs.Port,
                UserName = _mqConfigs.User,
                Password = _mqConfigs.Password
            };
            if (_connectionSend != null && _connectionSend.IsOpen)
            {
                return; //已有连接
            }

            _connectionSend = _connectionSendFactory.CreateConnection(); //创建生产者连接

            if (_modelSend != null && _modelSend.IsOpen)
            {
                return; //已有通道
            }

            _modelSend = _connectionSend.CreateModel(); //创建生产者通道

            // 声明主交换机 为 Fanout 类型，持久化
            _modelSend.ExchangeDeclare(
                exchange: _mqConfigs.ExchangeName,
                type: ExchangeType.Fanout,
                durable: true, // 明确设置为持久化
                autoDelete: false,
                arguments: null
            );

            // 声明死信交换机 为Fanout类型，持久化
            _modelSend.ExchangeDeclare(
                exchange: _mqConfigs.DeadLetterExchangeName,
                type: ExchangeType.Fanout,
                durable: true, // 明确设置为持久化
                autoDelete: false,
                arguments: null
            );

            // 声明死信队列
            _modelSend.QueueDeclare(
                queue: _mqConfigs.DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // 绑定死信队列到死信交换机
            _modelSend.QueueBind(_mqConfigs.DeadLetterQueueName, _mqConfigs.DeadLetterExchangeName, routingKey: "");

            Console.WriteLine("【结束】>>>>>>>>>>>>>>>生产者连接");
        }


        /// <summary>
        /// 消息发布到交换机（Fanout模式）
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="exchangeName">交换机名称</param>
        /// <returns>发布结果</returns>
        public async Task<(bool Success, string ErrorMessage)> PublishAsync(string message, string exchangeName)
        {
            try
            {
                byte[] body = Encoding.UTF8.GetBytes(message);

                await Task.Run(() =>
                {
                    _modelSend.BasicPublish(
                        exchange: exchangeName,
                        routingKey: string.Empty, // Fanout 模式无需 RoutingKey
                        basicProperties: null,
                        body: body
                    );
                });

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"发布消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 消费者初始化连接配置
        /// </summary>
        public void InitConsumerConnections(List<QueueConfigInfo> queueConfigs)
        {
            Console.WriteLine("【开始】>>>>>>>>>>>>>>>消费者连接");

            //创建单个连接工厂
            _connectionReceiveFactory = new ConnectionFactory
            {
                HostName = _mqConfigs.Host,
                Port = _mqConfigs.Port,
                UserName = _mqConfigs.User,
                Password = _mqConfigs.Password
            };
            _costomerCount = queueConfigs.Sum(q => q.ConsumerCount); // 获取所有队列的消费者总数

            // 初始化数组         
            _connectionReceive = new IConnection[_costomerCount];
            _modelReceive = new IModel[_costomerCount];
            _basicConsumer = new EventingBasicConsumer[_costomerCount];

            int consumerIndex = 0; // 用于跟踪当前消费者索引

            foreach (var queueConfig in queueConfigs)
            {
                for (int i = 0; i < queueConfig.ConsumerCount; i++)
                {
                    string queueName = queueConfig.QueueName;

                    // 创建连接
                    _connectionReceive[consumerIndex] = _connectionReceiveFactory.CreateConnection();
                    _modelReceive[consumerIndex] = _connectionReceive[consumerIndex].CreateModel();
                    _basicConsumer[consumerIndex] = new EventingBasicConsumer(_modelReceive[consumerIndex]);

                    // 声明主交换机（确保交换机存在）
                    _modelReceive[consumerIndex].ExchangeDeclare(_mqConfigs.ExchangeName, ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null);

                    // 声明死信交换机为 Fanout 类型
                    _modelReceive[consumerIndex].ExchangeDeclare(_mqConfigs.DeadLetterExchangeName, ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null);

                    if (queueName == _mqConfigs.DeadLetterQueueName)
                    {
                        // 死信队列的声明和绑定
                        _modelReceive[consumerIndex].QueueDeclare(
                            queue: queueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null
                        );

                        // 只将死信队列绑定到死信交换机
                        _modelReceive[consumerIndex].QueueBind(queueName, _mqConfigs.DeadLetterExchangeName, routingKey: "");
                    }
                    else
                    {
                        // 业务队列的声明和绑定
                        _modelReceive[consumerIndex].QueueDeclare(
                            queue: queueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: new Dictionary<string, object>
                            {
                        { "x-dead-letter-exchange", _mqConfigs.DeadLetterExchangeName },
                        { "x-dead-letter-routing-key", "" }
                            }
                        );

                        // 只将业务队列绑定到主交换机
                        _modelReceive[consumerIndex].QueueBind(queueName, _mqConfigs.ExchangeName, routingKey: "");
                    }

                    

                    // 设置QoS，确保每次只处理一个消息
                    _modelReceive[consumerIndex].BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                    consumerIndex++;
                }
            }

            Console.WriteLine("【结束】>>>>>>>>>>>>>>>消费者连接初始化完成");
        }

        /// <summary>
        /// 消费者连接
        /// </summary>
        public async Task ConncetionReceive(int consumeIndex, string exchangeName, string queueName, Func<string, Task> action)
        {

            await StartListenerAsync(async (model, ea) =>
            {
                try
                {
                    byte[] message = ea.Body.ToArray();
                    string msg = Encoding.UTF8.GetString(message);
                    Console.WriteLine($"队列 {queueName}，消费者索引 {consumeIndex} 接收到消息：{msg}");

                    await action(msg);
                    _modelReceive[consumeIndex].BasicAck(ea.DeliveryTag, true);//确认消息
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理消息时发生错误: {ex.Message}");
                    // 拒绝消息且不重新入队，触发死信机制
                    _modelReceive[consumeIndex].BasicNack(ea.DeliveryTag, false, false);
                }

            }, queueName, consumeIndex);
        }

        /// <summary>
        /// 手动确认消费机制
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="queueName"></param>
        /// <param name="consumeIndex"></param>
        /// <returns></returns>
        private async Task StartListenerAsync(AsyncEventHandler<BasicDeliverEventArgs> handler, string queueName, int consumeIndex)
        {
            _basicConsumer[consumeIndex].Received += async (sender, ea) => await handler(sender, ea);
            _modelReceive[consumeIndex].BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: _basicConsumer[consumeIndex]
            );

            Console.WriteLine($"队列 {queueName} 的消费者 {consumeIndex} 已启动监听");
        }


    }
}
