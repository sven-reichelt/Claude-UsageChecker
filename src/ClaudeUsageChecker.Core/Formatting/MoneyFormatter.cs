using System.Globalization;

namespace ClaudeUsageChecker.Core.Formatting;

/// <summary>
/// Writes an amount of money the way the interface language writes numbers,
/// followed by the currency the API named.
/// </summary>
/// <remarks>
/// <para>
/// The currency is not the application's to decide. An account billed in euros
/// reports EUR, one in the United States USD, one in Brazil BRL - and the number
/// of decimal places belongs to the currency as well, which is why the endpoint
/// sends an exponent instead of assuming two. Both are taken from the response
/// and nothing is hard-wired.
/// </para>
/// <para>
/// The ISO code is written out rather than translated into a symbol. There is no
/// dependable mapping from code to symbol - "$" stands for a dozen currencies,
/// and showing a Brazilian amount with a US dollar sign would be worse than
/// showing no symbol at all. "22,76 EUR" is understood everywhere and cannot
/// mislead.
/// </para>
/// <para>
/// The separator and grouping follow the current culture, which follows the
/// interface language: a German reading gets "1.234,50 EUR", an English one
/// "1,234.50 EUR".
/// </para>
/// </remarks>
public static class MoneyFormatter
{
    /// <summary>Two places where the API says nothing else.</summary>
    private const int DefaultDecimals = 2;

    /// <summary>
    /// Formats <paramref name="amount"/> with <paramref name="currency"/>.
    /// </summary>
    /// <param name="amount">The amount, in whole currency units.</param>
    /// <param name="currency">The ISO code, or null where the API named none.</param>
    /// <param name="decimals">How many decimal places the currency carries.</param>
    public static string Format(decimal amount, string? currency, int? decimals = null)
    {
        var places = decimals is >= 0 and <= 6 ? decimals.Value : DefaultDecimals;
        var number = amount.ToString("N" + places.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture);

        return string.IsNullOrWhiteSpace(currency) ? number : number + " " + currency;
    }

    /// <summary>
    /// Turns an amount in the smallest unit into whole units: 2276 with an
    /// exponent of 2 becomes 22.76.
    /// </summary>
    /// <remarks>
    /// Without an exponent the number is taken as it stands. Assuming two places
    /// would turn a limit of 50 into 0.50, and quietly understating what someone
    /// is allowed to spend is the worse mistake.
    /// </remarks>
    public static decimal FromMinorUnits(decimal amountMinor, int? exponent)
    {
        if (exponent is not (> 0 and <= 6))
        {
            return amountMinor;
        }

        var divisor = 1m;
        for (var i = 0; i < exponent.Value; i++)
        {
            divisor *= 10m;
        }

        return amountMinor / divisor;
    }
}
