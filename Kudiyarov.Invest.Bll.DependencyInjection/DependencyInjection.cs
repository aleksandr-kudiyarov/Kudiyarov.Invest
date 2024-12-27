using Kudiyarov.Invest.Bll.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Kudiyarov.Invest.Bll.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInvestLogicDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IInvestLogic, InvestLogic>();
    }
}