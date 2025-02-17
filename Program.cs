using System.Threading;
using AntiFraudMicroservice.Data;
using AntiFraudMicroservice.Services;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<AntiFraudConsumerService>();
builder.Services.AddScoped<TransactionStatusConsumerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var cancellationTokenSource = new CancellationTokenSource();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var scope = app.Services.CreateScope();
    var antiFraudConsumerService =
        scope.ServiceProvider.GetRequiredService<AntiFraudConsumerService>();
    var transactionStatusConsumerService =
        scope.ServiceProvider.GetRequiredService<TransactionStatusConsumerService>();

    Task.Run(() => antiFraudConsumerService.StartConsuming(cancellationTokenSource.Token));
    Task.Run(() => transactionStatusConsumerService.StartConsuming(cancellationTokenSource.Token));
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    cancellationTokenSource.Cancel();
});

app.Run();
