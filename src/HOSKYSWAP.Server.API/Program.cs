using System.Net.Http.Headers;
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

app.MapGet("/parameters", async () => await bfEpochService.GetLatestParametersAsync());

app.MapGet("/blocks/latest", async () => await bfBlockService.GetLatestAsync());

app.MapPost("/tx/submit", async (RequestDelegate) => {
    using var reader = new StreamReader(RequestDelegate.Request.Body);
    using var memStream = new MemoryStream();
    using var httpClient = new HttpClient();
    await reader.BaseStream.CopyToAsync(memStream);

    httpClient.DefaultRequestHeaders.Add("project_id", blockfrostProjectID);
    var byteContent = new ByteArrayContent(memStream.ToArray());
    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");
    var txResponse = await httpClient.PostAsync($"{blockfrostAPI}/tx/submit", byteContent);
    var txId = await txResponse.Content.ReadAsStringAsync();
});

app.Run();
