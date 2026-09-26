using Amqp.Mock.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRabbit(builder.Configuration);

var host = builder.Build();
host.Run();
