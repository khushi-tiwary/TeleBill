// Lets this file use UsageRecord (from the Domain folder)
using TeleBill.Domain;

// This file belongs to the Rating group
namespace TeleBill.Rating;


// A promise (like a USB-C port): every rule MUST have a Description and a Rate method.
public interface IRatingRule
{
    // Short text about the rule, shown in the log, e.g. "flat Rs.0.50/unit"
    string Description { get; }

    // Works out the charge for one record.
    // unitsUsedBefore = how much of this type the customer already used this month.
    decimal Rate(UsageRecord record, decimal unitsUsedBefore);
}

// Same price for every unit. ": IRatingRule" = this class keeps the promise.
public class FlatRateRule : IRatingRule
{
    // Price of one unit (1 minute, 1 SMS or 1 MB)
    public decimal RatePerUnit { get; }

    public FlatRateRule(decimal ratePerUnit)
    {
        // A price can't be negative
        if (ratePerUnit < 0)
            throw new ArgumentException("Rate cannot be negative");
        RatePerUnit = ratePerUnit;
    }

    // Text shown in the log, e.g. "flat Rs.0.5/unit"
    public string Description => $"flat Rs.{RatePerUnit}/unit";

    // Units x price. Flat doesn't care about history, so unitsUsedBefore isn't used here.
    public decimal Rate(UsageRecord record, decimal unitsUsedBefore)
        => Money(record.Units * RatePerUnit);

    // Rounds to 2 decimal places (paise). internal = other classes in this project can use it.
    internal static decimal Money(decimal amount) => Math.Round(amount, 2);
}

// First TierLimit units at one price, the rest at a cheaper price.
// Example: first 100 min at Rs.1, after that Rs.0.60
public class TieredRateRule : IRatingRule
{
    public decimal TierLimit { get; }       // size of the first tier, e.g. 100 minutes
    public decimal FirstTierRate { get; }   // price inside the first tier, e.g. Rs.1
    public decimal AfterTierRate { get; }   // price after the first tier is used up, e.g. Rs.0.60

    public TieredRateRule(decimal tierLimit, decimal firstTierRate, decimal afterTierRate)
    {
        // The tier must have some size, and prices can't be negative
        if (tierLimit <= 0)
            throw new ArgumentException("Tier limit must be more than 0");
        if (firstTierRate < 0 || afterTierRate < 0)
            throw new ArgumentException("Rates cannot be negative");

        TierLimit = tierLimit;
        FirstTierRate = firstTierRate;
        AfterTierRate = afterTierRate;
    }

    public string Description => $"first {TierLimit} @ Rs.{FirstTierRate}, then Rs.{AfterTierRate}";

    public decimal Rate(UsageRecord record, decimal unitsUsedBefore)
    {
        // How much of the first tier is still left. Math.Max(0, ...) stops it going below 0.
        // Example: limit 100, already used 90 -> 10 left
        decimal tierLeft = Math.Max(0, TierLimit - unitsUsedBefore);

        // Part of this record that fits in the first tier
        // Example: call of 20 min, only 10 left -> 10 in first tier
        decimal inFirstTier = Math.Min(record.Units, tierLeft);

        // The rest goes in the second tier. Example: 20 - 10 = 10
        decimal inSecondTier = record.Units - inFirstTier;

        // Add both parts and round. Example: 10 x 1.00 + 10 x 0.60 = Rs.16
        return FlatRateRule.Money(inFirstTier * FirstTierRate + inSecondTier * AfterTierRate);
    }
}

// First FreeUnits are free, after that each unit is charged.
// Example: 300 free minutes, then Rs.0.50 per minute
public class FreeAllowanceRule : IRatingRule
{
    public decimal FreeUnits { get; }       // e.g. 300 free minutes
    public decimal RateAfterFree { get; }   // price once the free units are used up

    public FreeAllowanceRule(decimal freeUnits, decimal rateAfterFree)
    {
        if (freeUnits < 0)
            throw new ArgumentException("Free units cannot be negative");
        if (rateAfterFree < 0)
            throw new ArgumentException("Rate cannot be negative");

        FreeUnits = freeUnits;
        RateAfterFree = rateAfterFree;
    }

    public string Description => $"first {FreeUnits} free, then Rs.{RateAfterFree}";

    public decimal Rate(UsageRecord record, decimal unitsUsedBefore)
    {
        // Free units still left this month. Example: 300 free, used 280 -> 20 left
        decimal freeLeft = Math.Max(0, FreeUnits - unitsUsedBefore);

        // Units NOT covered by the free part. Example: call of 50 min, 20 free -> 30 to pay
        decimal chargeable = Math.Max(0, record.Units - freeLeft);

        // Example: 30 x 0.50 = Rs.15
        return FlatRateRule.Money(chargeable * RateAfterFree);
    }
}

// Price depends on the hour the usage happened. Daytime costs more, night is cheaper.
public class PeakOffPeakRule : IRatingRule
{
    public decimal PeakRate { get; }      // daytime price
    public decimal OffPeakRate { get; }   // night price
    public int PeakStartHour { get; }     // peak starts at this hour (default 9 = 9 AM)
    public int PeakEndHour { get; }       // peak ends at this hour (default 21 = 9 PM)

    // "= 9" and "= 21" are default values, so you can skip them when creating the rule
    public PeakOffPeakRule(decimal peakRate, decimal offPeakRate, int peakStartHour = 9, int peakEndHour = 21)
    {
        if (peakRate < 0 || offPeakRate < 0)
            throw new ArgumentException("Rates cannot be negative");

        // Hours must be 0-24, and the start must come before the end
        if (peakStartHour < 0 || peakEndHour > 24 || peakStartHour >= peakEndHour)
            throw new ArgumentException("Peak hours are not valid");

        PeakRate = peakRate;
        OffPeakRate = offPeakRate;
        PeakStartHour = peakStartHour;
        PeakEndHour = peakEndHour;
    }

    public string Description => $"Rs.{PeakRate} peak ({PeakStartHour}:00-{PeakEndHour}:00), Rs.{OffPeakRate} off-peak";

    public decimal Rate(UsageRecord record, decimal unitsUsedBefore)
    {
        // Hour the usage happened, 0 to 23
        int hour = record.Timestamp.Hour;

        // true if the hour falls between the peak start and end (9 AM to 9 PM)
        bool isPeak = hour >= PeakStartHour && hour < PeakEndHour;

        // ? : is a short if/else: if peak use PeakRate, otherwise use OffPeakRate
        decimal rate = isPeak ? PeakRate : OffPeakRate;

        // Example: 10 min at 10 AM -> 10 x 0.80 = Rs.8. At 11 PM -> 10 x 0.20 = Rs.2
        return FlatRateRule.Money(record.Units * rate);
    }
}
