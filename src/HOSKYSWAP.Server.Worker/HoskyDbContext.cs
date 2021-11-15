using Microsoft.EntityFrameworkCore;

namespace HOSKYSWAP.Server.Worker;

public class HoskyDbContext : DbContext
{
    public DbSet<Order>? Orders { get; set; }

    public HoskyDbContext(DbContextOptions<HoskyDbContext> options) : base(options) { }
}