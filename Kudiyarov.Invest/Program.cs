using Kudiyarov.Invest.Common.Configuration;
using Kudiyarov.Invest.Extensions;
using Kudiyarov.Invest.TinkoffClient.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var tinkoffConfiguration = builder.Configuration.GetRequiredValue<TinkoffConfiguration>("TinkoffConfiguration");
builder.Services.AddTinkoffClient(tinkoffConfiguration);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();