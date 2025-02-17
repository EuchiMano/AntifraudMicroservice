using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AntiFraudMicroservice.Data;
using AntiFraudMicroservice.Models;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AntiFraudMicroservice.Services
{
    public class AntiFraudConsumerService
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly KafkaProducerService _kafkaProducer;
        private readonly ApplicationDbContext _context;

        public AntiFraudConsumerService(
            KafkaProducerService kafkaProducer,
            ApplicationDbContext context
        )
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "anti-fraud-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            _kafkaProducer = kafkaProducer;
            _context = context;
        }

        public async Task StartConsuming(CancellationToken cancellationToken)
        {
            _consumer.Subscribe("transaction-created");

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = _consumer.Consume(cancellationToken);
                var transaction = JsonConvert.DeserializeObject<Transaction>(result.Message.Value);
                await ValidateTransaction(transaction);
            }
        }

        private async Task ValidateTransaction(Transaction transaction)
        {
            string status;

            if (transaction.Value > 2000)
            {
                status = "rejected";
            }
            else
            {
                var dailyTotal = await CalculateDailyTotalAsync(transaction.CreatedAt.Date);
                if (dailyTotal + transaction.Value > 20000)
                {
                    status = "rejected";
                }
                else
                {
                    status = "approved";
                }
            }

            var statusEvent = new { TransactionId = transaction.Id, Status = status };

            await _kafkaProducer.ProduceAsync(
                $"transaction-status-{status.ToLower()}",
                JsonConvert.SerializeObject(statusEvent)
            );
        }

        private async Task<decimal> CalculateDailyTotalAsync(DateTime date)
        {
            var dailyTotal = await _context
                .Transactions.Where(t => t.CreatedAt.Date == date.Date && t.Status == "approved")
                .SumAsync(t => t.Value);
            return dailyTotal;
        }
    }
}
