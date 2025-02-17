using System.Threading;
using AntiFraudMicroservice.Services;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace AntiFraudMicroservice.Services
{
    public class TransactionStatusConsumerService
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly TransactionService _transactionService;

        public TransactionStatusConsumerService(TransactionService transactionService)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "transaction-status-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            _transactionService = transactionService;
        }

        public async Task StartConsuming(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(
                new[] { "transaction-status-approved", "transaction-status-rejected" }
            );

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = _consumer.Consume(cancellationToken);

                if (
                    result.Topic == "transaction-status-approved"
                    || result.Topic == "transaction-status-rejected"
                )
                {
                    var statusEvent = JsonConvert.DeserializeObject<TransactionStatusEvent>(
                        result.Message.Value
                    );

                    await _transactionService.UpdateTransactionStatusAsync(
                        statusEvent.TransactionId,
                        statusEvent.Status
                    );
                }
            }
        }
    }

    public class TransactionStatusEvent
    {
        public Guid TransactionId { get; set; }
        public string Status { get; set; }
    }
}
