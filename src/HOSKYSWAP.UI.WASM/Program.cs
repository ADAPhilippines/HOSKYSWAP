using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HOSKYSWAP.UI.WASM;
using HOSKYSWAP.UI.WASM.Services;
using HOSKYSWAP.UI.WASM.Services.JSInterop;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CardanoWalletInteropService>();
builder.Services.AddScoped<HelperInteropService>();
builder.Services.AddScoped<AppStateService>();
builder.Services.AddScoped<BackendService>(); 

builder.Services.AddMudServices();
await builder.Build().RunAsync();
