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
            // 模拟失败条件
            if (msg.Contains("fail"))
            {
                throw new Exception("Simulated processing failure in ExActionOne");
            }
            await Task.Delay(1000); // 替换 Thread.Sleep
        }
        /// <summary>
        /// 库存扣减
        /// </summary>
        public async Task ExActionTwo(string msg)
        {
            Console.WriteLine($"消费成功了【{msg}】消息以后正在执行库存扣减操作");
            // 模拟失败条件
            if (msg.Contains("fail"))
            {
                throw new Exception("Simulated processing failure in ExActionTwo");
            }
            await Task.Delay(1000); // 替换 Thread.Sleep
        }
        /// <summary>
        /// 处理死信队列的消息
        /// </summary>
        public async Task ExActionDeadLetter(string message)
        {
            Console.WriteLine($"处理死信消息: {message}");
            // 在这里可以记录日志、发送通知等
            await Task.Delay(1000);
        }
    }
}
