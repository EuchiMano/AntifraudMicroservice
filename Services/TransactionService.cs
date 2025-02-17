using AntiFraudMicroservice.Data;
using AntiFraudMicroservice.Models;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace AntiFraudMicroservice.Services
{
    public class TransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly KafkaProducerService _kafkaProducer;

        public TransactionService(ApplicationDbContext context, KafkaProducerService kafkaProducer)
        {
            _context = context;
            _kafkaProducer = kafkaProducer;
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _kafkaProducer.ProduceAsync(
                "transaction-created",
                JsonConvert.SerializeObject(transaction)
            );

            return transaction;
        }

        public async Task UpdateTransactionStatusAsync(Guid transactionId, string status)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
            {
                throw new Exception("Transaction not found");
            }

            transaction.Status = status;
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
