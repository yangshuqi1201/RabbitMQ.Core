using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Model
{
    public class QueueConfigInfo
    {
        public string QueueName { get; set; }
        public int ConsumerCount { get; set; }
    }
}
