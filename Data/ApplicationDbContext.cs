using AntiFraudMicroservice.Models;
using Microsoft.EntityFrameworkCore;

namespace AntiFraudMicroservice.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Add this constructor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
    }
}
