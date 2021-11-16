using System.Net.Http.Headers;
using Blockfrost.Api.Extensions;
using Blockfrost.Api.Models;
using Blockfrost.Api.Services;
using Microsoft.AspNetCore.Mvc;
using HOSKYSWAP.Data;
using Microsoft.EntityFrameworkCore;
using HOSKYSWAP.Common;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

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

string connectionString = builder.Configuration.GetConnectionString("HOSKYSWAPDB");
var blockfrostProjectID = builder.Configuration["BlockfrostProjectID"];
var blockfrostAPI = builder.Configuration["BlockfrostAPI"];
var cardanoNetwork = builder.Configuration["CardanoNetwork"];

builder.Services.AddDbContext<HoskyDbContext>(options => options.UseNpgsql(connectionString), contextLifetime: ServiceLifetime.Singleton);
builder.Services.AddBlockfrost(cardanoNetwork, blockfrostProjectID);
var app = builder.Build();
app.UseCors("AllowAll");

var _logger = app.Logger;

app.MapGet("/parameters", async (IEpochsService bfEpochService) => await bfEpochService.GetLatestParametersAsync());

app.MapGet("/blocks/latest", async (IBlocksService bfBlockService) => await bfBlockService.GetLatestAsync());

app.MapGet("/txs/{hash}", async ([FromRoute] string hash, ITransactionsService bfTxService) => await bfTxService.GetAsync(hash));

app.MapGet("/order/last/execute", async (HoskyDbContext dbContext) =>
{
    if (dbContext.Orders is not null)
    {
        return await dbContext.Orders.Where(o => o.Status == Status.Filled).OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
    }
    else
        throw new Exception("Hello World");
});

app.MapPost("/tx/submit", SumbitTx);

_logger.LogInformation($"API Server running at: {DateTimeOffset.Now}");
app.Run();

#region DELEGATES

async Task SumbitTx(HttpContext ctx)
{
    try
    {
        using var reader = new StreamReader(ctx.Request.Body);
        using var memStream = new MemoryStream();
        using var httpClient = new HttpClient();
        await reader.BaseStream.CopyToAsync(memStream);

        httpClient.DefaultRequestHeaders.Add("project_id", blockfrostProjectID);
        var byteContent = new ByteArrayContent(memStream.ToArray());
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");
        var txResponse = await httpClient.PostAsync($"{blockfrostAPI}/tx/submit", byteContent);
        var txId = await txResponse.Content.ReadAsStringAsync();
        txId = txId.Replace("\"", string.Empty);
        Console.WriteLine(txId);

        if (txId.Length != 64) throw new Exception(txId);

        await ctx.Response.WriteAsJsonAsync(new { status = 200, result = txId });
    }
    catch (Exception e)
    {
        _logger.LogError($"Error in fetching transaction: {e}");
        await ctx.Response.WriteAsJsonAsync(new { error = true, message = e });
    }
}


#endregion