using HOSKYSWAP.Data;
using HOSKYSWAP.Server.Worker;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();
        string connectionString = hostContext.Configuration.GetConnectionString("HOSKYSWAPDB");
        services.AddDbContext<HoskyDbContext>(options => options.UseNpgsql(connectionString), contextLifetime: ServiceLifetime.Singleton);
    })
    .Build();

await host.RunAsync();
