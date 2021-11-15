using Blockfrost.Api.Extensions;
using Blockfrost.Api.Services;
using HOSKYSWAP.Common;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAll",
        builder =>
        {
            builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
        }
    );
});

var app = builder.Build();
 app.UseCors("AllowAll");

var blockfrostProjectID = app.Configuration["BlockfrostProjectID"];
var cardanoNetwork = app.Configuration["CardanoNetwork"];

// Blockfrost API
var blockFrostProvider = new ServiceCollection()
    .AddBlockfrost(
        cardanoNetwork,
        blockfrostProjectID)
    .BuildServiceProvider();

var bfEpochService = blockFrostProvider.GetRequiredService<IEpochsService>();
var bfBlockService = blockFrostProvider.GetRequiredService<IBlocksService>();

app.MapGet("/parameters", async () => await bfEpochService.GetLatestParametersAsync());

app.MapGet("/blocks/latest", async () => await bfBlockService.GetLatestAsync());

app.Run();
