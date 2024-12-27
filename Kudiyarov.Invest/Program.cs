using Kudiyarov.Invest.Bll.DependencyInjection;
using Kudiyarov.Invest.Bll.Interfaces;
using Kudiyarov.Invest.Common.Configuration;
using Kudiyarov.Invest.Extensions;
using Kudiyarov.Invest.Dal.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var tinkoffConfiguration = builder.Configuration.GetRequiredValue<TinkoffConfiguration>("TinkoffConfiguration");
builder.Services.AddInvestLogicDependencies();
builder.Services.AddInvestClientDependencies(tinkoffConfiguration);
builder.Services.AddMemoryCache();

var app = builder.Build();

var client = app.Services.GetRequiredService<IInvestLogic>();
await client.Do();