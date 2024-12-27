namespace Kudiyarov.Invest.Common.Configuration;

public record TinkoffConfiguration
{
    public required string Token { get; init; }
    public required string PrimaryAccount { get; init; }
    public required string SecondaryAccount { get; init; }
}