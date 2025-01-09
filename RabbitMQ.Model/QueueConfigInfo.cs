using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Model
{
    public class QueueConfigInfo
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; set; }
        /// <summary>
        /// 消费者数量
        /// </summary>
        public int ConsumerCount { get; set; }
    }
}
