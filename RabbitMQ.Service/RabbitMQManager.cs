using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using RabbitMQ.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        public RabbitMQManager(string hostname, int port, string username, string password, string exchangeName)
        {
            _mqConfigs = new MqConfigInfo { Host = hostname, Port = port, User = username, Password = password, ExchangeName = exchangeName };
        }

        /// <summary>
        /// 初始化生产者连接
        /// </summary>
        public void InitProducerConnection()
        {
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

            // 修改为 Fanout 类型
            _modelSend.ExchangeDeclare(_mqConfigs.ExchangeName, ExchangeType.Fanout);
        }


        /// <summary>
        /// 消息发布到交换机（Fanout模式）
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="exchangeName">交换机名称</param>
        /// <returns>发布结果</returns>
        public async Task<(bool Success, string ErrorMessage)> PublishAsync(string message,string exchangeName)
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

            

           
        }

        /// <summary>
        /// 消费者连接
        /// </summary>
        /// <param name="consumeIndex"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routeKey"></param>
        /// <param name="queueName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <summary>
        /// 消费者连接
        /// </summary>
        public async Task ConncetionReceive(int consumeIndex, string exchangeName, string queueName, Func<string, Task> action)
        {
            Console.WriteLine($"开始连接RabbitMQ消费者：【队列】 {queueName}，【消费者索引】 {consumeIndex}");
            if (_connectionReceive[consumeIndex] != null && _connectionReceive[consumeIndex].IsOpen)
            {
                Console.WriteLine($"消费者 {consumeIndex} 已经连接，无需重新连接");
                return;
            }

            _connectionReceive[consumeIndex] = _connectionReceiveFactory.CreateConnection();
            _modelReceive[consumeIndex] = _connectionReceive[consumeIndex].CreateModel();
            _basicConsumer[consumeIndex] = new EventingBasicConsumer(_modelReceive[consumeIndex]);

            // 修改为 Fanout 类型
            _modelReceive[consumeIndex].ExchangeDeclare(exchangeName, ExchangeType.Fanout);

            // 在发布订阅模式下，QueueName仍然需要绑定，但RoutingKey不再使用
            _modelReceive[consumeIndex].QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _modelReceive[consumeIndex].QueueBind(queueName, exchangeName, string.Empty); // 不使用 RoutingKey
            _modelReceive[consumeIndex].BasicQos(0, 1, false);

            await StartListenerAsync(async (model, ea) =>
            {
                byte[] message = ea.Body.ToArray();
                string msg = Encoding.UTF8.GetString(message);
                Console.WriteLine($"队列 {queueName}，消费者索引 {consumeIndex} 接收到消息：{msg}");

                await action(msg);
                _modelReceive[consumeIndex].BasicAck(ea.DeliveryTag, true);
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
