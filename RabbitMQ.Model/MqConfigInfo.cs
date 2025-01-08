﻿namespace RabbitMQ.Model
{
    public class MqConfigInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string ExchangeName { get; set; }
        public string DeadLetterExchangeName { get; set; }
        public string DeadLetterQueueName { get; set; }
    }
}
