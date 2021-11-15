using HOSKYSWAP.Server.Worker;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        string connectionString = hostContext.Configuration.GetConnectionString("HOSKYSWAPDB");
        services.AddHostedService<Worker>();
        services.AddDbContext<HoskyDbContext>(options => options.UseNpgsql(connectionString), contextLifetime: ServiceLifetime.Singleton);
    })
    .Build();

await host.RunAsync();
