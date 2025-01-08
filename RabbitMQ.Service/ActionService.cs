using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Service
{
    public class ActionService
    {
        /// <summary>
        /// 付款
        /// </summary>
        public async Task ExActionOne(string msg)
        {      
            Console.WriteLine($"消费成功了【{msg}】消息以后正在执行付款操作");
            await Task.Delay(1000); // 替换 Thread.Sleep
        }
        /// <summary>
        /// 库存扣减
        /// </summary>
        public async Task ExActionTwo(string msg)
        {
            Console.WriteLine($"消费成功了【{msg}】消息以后正在执行库存扣减操作");
            await Task.Delay(1000); // 替换 Thread.Sleep
        }
    }
}
