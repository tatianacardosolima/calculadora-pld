using System.Globalization;
using Calculadora.Web;
using Calculadora.Web.Servicos;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// RNF06: números, datas e moeda no formato brasileiro.
var ptBr = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<DadosPld>();

await builder.Build().RunAsync();
