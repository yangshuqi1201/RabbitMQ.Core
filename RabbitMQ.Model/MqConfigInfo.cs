namespace RabbitMQ.Model
{
    public class MqConfigInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// 主交换机
        /// </summary>
        public string ExchangeName { get; set; }
        /// <summary>
        /// 死信交换机
        /// </summary>
        public string DeadLetterExchangeName { get; set; }
        /// <summary>
        /// 死信队列名称
        /// </summary>
        public string DeadLetterQueueName { get; set; }
    }
}
