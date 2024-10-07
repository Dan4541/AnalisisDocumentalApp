using Microsoft.EntityFrameworkCore;
using AnalysisDocumentalApp.Models;

namespace AnalysisDocumentalApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        
        public DbSet<Document> Documents { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
