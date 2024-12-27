using Kudiyarov.Invest.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tinkoff.InvestApi;

namespace Kudiyarov.Invest.TinkoffClient.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static void AddTinkoffClient(
        this IServiceCollection services,
        TinkoffConfiguration configuration)
    {
        var client = InvestApiClientFactory.Create(configuration.Token);
        services.AddSingleton(client);
    }
}