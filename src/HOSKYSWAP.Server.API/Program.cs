using System.Net.Http.Headers;
using Blockfrost.Api.Extensions;
using Blockfrost.Api.Models;
using Blockfrost.Api.Services;
using Microsoft.AspNetCore.Mvc;

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

var app = builder.Build();
 app.UseCors("AllowAll");

var blockfrostProjectID = app.Configuration["BlockfrostProjectID"];
var blockfrostAPI = app.Configuration["BlockfrostAPI"];
var cardanoNetwork = app.Configuration["CardanoNetwork"];

// Blockfrost API
var blockFrostProvider = new ServiceCollection()
    .AddBlockfrost(
        cardanoNetwork,
        blockfrostProjectID)
    .BuildServiceProvider();

var bfEpochService = blockFrostProvider.GetRequiredService<IEpochsService>();
var bfBlockService = blockFrostProvider.GetRequiredService<IBlocksService>();
var bfTxService = blockFrostProvider.GetRequiredService<ITransactionsService>();


var _logger = app.Logger;
app.MapGet("/parameters", async () => await ExecuteSafelyAsync<EpochParamContentResponse>(bfEpochService.GetLatestParametersAsync()));

app.MapGet("/blocks/latest", async () => await ExecuteSafelyAsync<BlockContentResponse>(bfBlockService.GetLatestAsync()));

app.MapGet("/txs/{hash}", async ([FromRoute]string hash) => await ExecuteSafelyAsync<TxContentResponse>(bfTxService.GetAsync(hash)));

app.MapPost("/tx/submit", SumbitTx);

_logger.LogInformation($"API Server running at: {DateTimeOffset.Now}");
app.Run();

#region DELEGATES
async Task<object?> ExecuteSafelyAsync<T>(Task<T> funcAsync)
{
    try
    {
        object? result = null;
        await Task.Run(() => {
            funcAsync.Wait();
            result = funcAsync.GetAwaiter().GetResult();
        });

        if(result is not null)
            return result;
        else
            return new { error = true };
    }
    catch (Exception e)
    {
        _logger.LogError($"Error in fetching transaction: {e}");
        return new { error = true, message = e.Message };
    }
};

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

        if(txId.Length != 64) throw new Exception(txId);

        await ctx.Response.WriteAsJsonAsync(new { status = 200, result = txId });
    }
    catch (Exception e)
    {
        _logger.LogError($"Error in fetching transaction: {e}");
        await ctx.Response.WriteAsJsonAsync(new { error = true, message = e });
    }
}
#endregion