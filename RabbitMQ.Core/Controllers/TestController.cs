using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Service;

namespace RabbitMQ.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly RabbitMQService _rabbitmqService;
        private readonly IConfiguration _configuration;

        public TestController(RabbitMQService rabbitmqService, IConfiguration configuration)
        {
            _rabbitmqService = rabbitmqService;
            _configuration = configuration;
        }
        /// <summary>
        /// 测试rabbitmq发送消息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> TestRabbitMqPublishMessage()
        {

            string pubMessage = "Hello, this is a broadcast message";

            var result = await _rabbitmqService.Instance.PublishAsync(
                pubMessage,
                _configuration["MQ:ExchangeName"]
            );

            if (!result.Success)
            {
                Console.WriteLine($"【生产者】消息发送失败：{result.ErrorMessage}");
            }

            Console.WriteLine("【生产者】消息发送完成");

            return Ok();
        }
    }
}
