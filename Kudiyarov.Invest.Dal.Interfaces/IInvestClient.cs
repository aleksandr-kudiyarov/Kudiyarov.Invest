using Tinkoff.InvestApi.V1;

namespace Kudiyarov.Invest.Dal.Interfaces;

public interface IInvestClient
{
    Task<PortfolioResponse> GetPortfolio(Account account);
    Task<IReadOnlyCollection<Account>> GetAccounts();
    Task<IReadOnlyCollection<Share>> GetShares();
    Task<IReadOnlyCollection<Etf>> GetEtfs();
    Task<IReadOnlyCollection<Currency>> GetCurrencies();
}