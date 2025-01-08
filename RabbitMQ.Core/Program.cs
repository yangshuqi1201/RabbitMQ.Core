using RabbitMQ.Model;
using RabbitMQ.Service;

var builder = WebApplication.CreateBuilder(args);
IConfiguration configuration = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<RabbitMQService>();
builder.Services.AddSingleton<ActionService>();

var app = builder.Build();

RabbitMQService rabbitMQService = app.Services.GetRequiredService<RabbitMQService>();
var consumer = app.Services.GetRequiredService<ActionService>();
//����1����������
await Task.Run(async () => {
    await rabbitMQService.Instance.ConncetionReceive(
        0,
        configuration["MQ:ExchangeName"],
        configuration["MQ:Queues:0:QueueName"],
        consumer.ExActionOne
    );
});
//����2����������
await Task.Run(async () => {
    await rabbitMQService.Instance.ConncetionReceive(
        1,
        configuration["MQ:ExchangeName"],
        configuration["MQ:Queues:1:QueueName"],
        consumer.ExActionTwo
    );
});

// �������Ŷ���������
await Task.Run(async () =>
{
    await rabbitMQService.Instance.ConncetionReceive(
        consumeIndex: 2, // ʹ��Ψһ������
        exchangeName: configuration["MQ:DeadLetterExchangeName"],
        queueName: configuration["MQ:Queues:2:QueueName"],
        action: consumer.ExActionDeadLetter
    );
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
