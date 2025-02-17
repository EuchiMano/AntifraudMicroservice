using AntiFraudMicroservice.Models;
using AntiFraudMicroservice.Services;
using Microsoft.AspNetCore.Mvc;

namespace AntiFraudMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly TransactionService _transactionService;

        public TransactionsController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] Transaction transaction)
        {
            transaction.Status = "pending";
            var createdTransaction = await _transactionService.CreateTransactionAsync(transaction);
            return Ok(createdTransaction);
        }
    }
}
