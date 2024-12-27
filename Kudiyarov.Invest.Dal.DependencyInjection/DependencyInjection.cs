using Kudiyarov.Invest.Common.Configuration;
using Kudiyarov.Invest.Dal.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Tinkoff.InvestApi;

namespace Kudiyarov.Invest.Dal.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInvestClientDependencies(
        this IServiceCollection services,
        TinkoffConfiguration configuration)
    {
        services.AddSingleton<IInvestClient, TinkoffClient.TinkoffClient>();
        
        var client = InvestApiClientFactory.Create(configuration.Token);
        services.AddSingleton(client);
        
        services.AddSingleton(configuration);
    }
}