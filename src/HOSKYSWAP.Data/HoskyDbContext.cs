using Microsoft.EntityFrameworkCore;
using HOSKYSWAP.Common;

namespace HOSKYSWAP.Data;

public class HoskyDbContext : DbContext
{
    public DbSet<Order>? Orders { get; set; }

    public HoskyDbContext(DbContextOptions<HoskyDbContext> options) : base(options) { }
}