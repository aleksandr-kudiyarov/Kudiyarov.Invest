using System.Collections.Frozen;
using Kudiyarov.Invest.Bll.Interfaces;
using Kudiyarov.Invest.Common.Configuration;
using Kudiyarov.Invest.Dal.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tinkoff.InvestApi.V1;

namespace Kudiyarov.Invest.Bll;

public class InvestLogic(
    IInvestClient client,
    TinkoffConfiguration configuration,
    IMemoryCache cache) : IInvestLogic
{
    private async Task<IReadOnlyDictionary<string, Share>> GetSharesMap()
    {
        var map = await cache.GetOrCreateAsync(
            "GetSharesMap",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await GetSharesMapImpl();
            });
        
        ArgumentNullException.ThrowIfNull(map);
        return map;
    }

    private async Task<IReadOnlyDictionary<string, Share>> GetSharesMapImpl()
    {
        var shares = await client.GetShares();
        var map = shares.ToFrozenDictionary(key => key.Figi);
        return map;
    }
    
    private async Task<IReadOnlyDictionary<string, Etf>> GetEtfsMap()
    {
        var map = await cache.GetOrCreateAsync(
            "GetEtfsMap",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await GetEtfsMapImpl();
            });
        
        ArgumentNullException.ThrowIfNull(map);
        return map;
    }
    
    private async Task<IReadOnlyDictionary<string, Etf>> GetEtfsMapImpl()
    {
        var etfs = await client.GetEtfs();
        var map = etfs.ToFrozenDictionary(key => key.Figi);
        return map;
    }
    
    private async Task<IReadOnlyDictionary<string, Currency>> GetCurrenciesMap()
    {
        var map = await cache.GetOrCreateAsync(
            "GetCurrenciesMap",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await GetCurrenciesMapImpl();
            });
        
        ArgumentNullException.ThrowIfNull(map);
        return map;
    }
    
    private async Task<IReadOnlyDictionary<string, Currency>> GetCurrenciesMapImpl()
    {
        var currencies = await client.GetCurrencies();
        var map = currencies.ToFrozenDictionary(key => key.Figi);
        return map;
    }

    private static IReadOnlyDictionary<string, PortfolioPosition> GetPositionsMap(
        IEnumerable<PortfolioPosition> positions)
    {
        var map = positions
            .ToDictionary(key => key.Figi);

        return map;
    }
    
    private async Task<IReadOnlyCollection<PortfolioPosition>> GetPrimaryPositions()
    {
        var account = await GetPrimaryAccount();
        var portfolio = await GetPositions(account, position => position.InstrumentType != "currency");
        return portfolio;
    }
    
    private async Task<IReadOnlyCollection<PortfolioPosition>> GetSecondaryPositions()
    {
        var account = await GetSecondaryAccount();
        var portfolio = await GetPositions(account);
        return portfolio;
    }
    
    private async Task<IReadOnlyCollection<PortfolioPosition>> GetPositions(
        Account account,
        Predicate<PortfolioPosition>? predicate = null)
    {
        var portfolio = await client.GetPortfolio(account);
        var positions = portfolio.Positions;

        if (predicate != null)
        {
            return positions
                .Where(position => predicate(position))
                .ToList();
        }

        return positions;
    }
    
    private async Task<Account> GetPrimaryAccount()
    {
        var account = await GetAccountByName(configuration.PrimaryAccount);
        return account;
    }

    private async Task<Account> GetSecondaryAccount()
    {
        var account = await GetAccountByName(configuration.SecondaryAccount);
        return account;
    }

    private async Task<Account> GetAccountByName(string name)
    {
        var map = await GetAccountsMap();
        map.TryGetValue(name, out var account);
        ArgumentNullException.ThrowIfNull(account);
        return account;
    }

    private async Task<IReadOnlyDictionary<string, Account>> GetAccountsMap()
    {
        var map = await cache.GetOrCreateAsync(
            "GetAccountsMap",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await GetAccountsMapImpl();
            });
        
        ArgumentNullException.ThrowIfNull(map);
        return map;
    }
    
    private async Task<IReadOnlyDictionary<string, Account>> GetAccountsMapImpl()
    {
        var accounts = await client.GetAccounts();

        var map = accounts
            .Where(account => account.Status == AccountStatus.Open)
            .ToFrozenDictionary(key => key.Name);

        return map;
    }
    
    //
    
    public async Task Do()
    {
        var primaryPositions = await GetPrimaryPositions();
        var secondaryPositions = await GetSecondaryPositions();

        MergePortfolios(ref primaryPositions, ref secondaryPositions);

        var join = primaryPositions.Join(secondaryPositions,
                l => l.Figi,
                r => r.Figi,
                (l, r) => (l, r))
            .ToList();
        
        var shares = await GetSharesMap();
        var etfs = await GetEtfsMap();
        var currencies = await GetCurrenciesMap();
        
        var finalPositions = TryToCalc(
            join,
            shares,
            etfs,
            currencies);

        foreach (var position in finalPositions.OrderByDescending(position => position.ToBuyRubles))
        {
            Console.WriteLine($"{position.Name}: {position.ToBuyLots:F2}");
        }
    }

    private static void MergePortfolios(
        ref IReadOnlyCollection<PortfolioPosition> primaryPortfolio,
        ref IReadOnlyCollection<PortfolioPosition> secondaryPortfolio)
    {
        var primaryPositionsMap = GetPositionsMap(primaryPortfolio);
        var secondaryPositionsMap = GetPositionsMap(secondaryPortfolio);
        
        var onlyPrimary = GetPositionsDiff(primaryPositionsMap, secondaryPositionsMap);
        var onlySecondary = GetPositionsDiff(secondaryPositionsMap, primaryPositionsMap);

        primaryPortfolio = primaryPortfolio.Concat(onlySecondary).ToArray();
        secondaryPortfolio = secondaryPortfolio.Concat(onlyPrimary).ToArray();
    }
    

    private IEnumerable<FinalPosition> TryToCalc(
        List<(PortfolioPosition l, PortfolioPosition r)> positions,
        IReadOnlyDictionary<string, Share> shares,
        IReadOnlyDictionary<string, Etf> etfs,
        IReadOnlyDictionary<string, Currency> currencies)
    {
        // взять цену б
        var rFullPrice = positions.Sum(po => po.r.Quantity * po.r.CurrentPrice);
        Console.WriteLine($"цена всего из ИИС: {rFullPrice}");
        
        // взять цену а
        var lFullPrice = positions.Sum(po => po.l.Quantity * po.l.CurrentPrice);
        Console.WriteLine($"цена всего из Следования: {lFullPrice}");
        
        // посчитать ratio
        var ratio = rFullPrice / lFullPrice;
        Console.WriteLine($"Ratio: {ratio:P}");
        Console.WriteLine();
        
        // для каждой позиции
        
        foreach (var position in positions)
        {
            var figi = position.l.Figi;
            
            shares.TryGetValue(figi, out var share);
            etfs.TryGetValue(figi, out var etf);
            currencies.TryGetValue(figi, out var currency);
            
            var name = share?.Name ?? etf?.Name ?? currency?.Name ?? "рубль???";

            var lot = share?.Lot ?? etf?.Lot ?? currency?.Lot ?? 1;
            

            Console.WriteLine($"Name: {name}");
            
            // умножить позицию а на ratio
            var mustBe = (position.l.CurrentPrice * position.l.Quantity) * ratio;
            Console.WriteLine($"Цена следования: ({position.l.CurrentPrice * position.l.Quantity})");
            Console.WriteLine($"Цена следования с пересчётом: {mustBe}");
            Console.WriteLine($"Цена в иис: {(position.r.CurrentPrice * position.r.Quantity)}");
            // вычесть из позиции б
            var rublesToBuy = mustBe - (position.r.CurrentPrice * position.r.Quantity);
            Console.WriteLine($"Докупить в ИИС: {rublesToBuy} в рублях");
            Console.WriteLine($"Докупить в ИИС: {rublesToBuy / position.l.CurrentPrice} в акциях");
            var lotsToBuy = rublesToBuy / position.l.CurrentPrice / lot;
            Console.WriteLine($"Докупить в ИИС: {lotsToBuy} в лотах");
            Console.WriteLine();

            var finalRecord = new FinalPosition
            {
                Name = name,
                ToBuyRubles = rublesToBuy,
                ToBuyLots = (int)Math.Round(lotsToBuy, MidpointRounding.AwayFromZero)
            };

            yield return finalRecord;
        }
    }

    private static IReadOnlyCollection<PortfolioPosition> GetPositionsDiff(
        IReadOnlyDictionary<string, PortfolioPosition> left,
        IReadOnlyDictionary<string, PortfolioPosition> right)
    {
        var result = left
            .Where(pair => right.ContainsKey(pair.Key) == false)
            .Select(pair => GetZeroPosition(pair.Value))
            .ToList();

        return result;
    }

    private static PortfolioPosition GetZeroPosition(PortfolioPosition position)
    {
        var zeroPosition = position.Clone();
        zeroPosition.Quantity = 0;
        return zeroPosition;
    }
}

public record FinalPosition
{
    public required string Name { get; set; }
    public required decimal ToBuyRubles { get; set; }
    public required int ToBuyLots { get; set; }
}
