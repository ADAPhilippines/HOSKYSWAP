using HOSKYSWAP.Data;
using HOSKYSWAP.Server.Worker;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((context, logging) =>
    {
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();
        string connectionString = hostContext.Configuration.GetConnectionString("HOSKYSWAPDB");
        services.AddDbContext<HoskyDbContext>(options => options.UseNpgsql(connectionString), contextLifetime: ServiceLifetime.Singleton);
    })
    .Build();

await host.RunAsync();
