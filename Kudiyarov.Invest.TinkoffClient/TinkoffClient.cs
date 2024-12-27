using Kudiyarov.Invest.Dal.Interfaces;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace Kudiyarov.Invest.TinkoffClient;

public class TinkoffClient(InvestApiClient client) : IInvestClient
{
    public async Task<PortfolioResponse> GetPortfolio(Account account)
    {
        var request = new PortfolioRequest
        {
            AccountId = account.Id,
        };

        var portfolio = await client.Operations.GetPortfolioAsync(request);
        return portfolio;
    }

    public async Task<IReadOnlyCollection<Account>> GetAccounts()
    {
        var request = new GetAccountsRequest();
        var response = await client.Users.GetAccountsAsync(request);
        var accounts = response.Accounts;
        return accounts;
    }

    public async Task<IReadOnlyCollection<Share>> GetShares()
    {
        var response = await client.Instruments.SharesAsync();
        var shares = response.Instruments;
        return shares;
    }
    
    public async Task<IReadOnlyCollection<Etf>> GetEtfs()
    {
        var response = await client.Instruments.EtfsAsync();
        var etfs = response.Instruments;
        return etfs;
    }
    
    public async Task<IReadOnlyCollection<Currency>> GetCurrencies()
    {
        var response = await client.Instruments.CurrenciesAsync();
        var currencies = response.Instruments;
        return currencies;
    }
}