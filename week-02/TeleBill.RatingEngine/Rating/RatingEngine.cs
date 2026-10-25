// Lets this file use Customer, Subscription, UsageRecord (Domain folder)
using TeleBill.Domain;

// This file belongs to the Rating group
namespace TeleBill.Rating;

// One priced usage = one line on the bill.
// record = a short class that just holds data. These 3 values become read only properties.
public record RatedLine(UsageRecord Record, string RuleUsed, decimal Charge);

// The full bill for one customer for one month.
public record Invoice(Subscription Subscription, IReadOnlyList<RatedLine> Lines, decimal UsageCharges)
{
    // Monthly rental comes from the customer's plan
    public decimal Rental => Subscription.Plan.MonthlyRental;

    // Final amount = rental + all usage charges
    public decimal Total => Rental + UsageCharges;
}

// Turns raw usage into money.
public class RatingEngine
{
    // EVENTS: the engine announces these. Anyone can subscribe with +=
    // Action<RatedLine> = a method that takes a RatedLine and returns nothing.
    // The ? means "can be null", because nobody may have subscribed yet.
    public event Action<RatedLine>? UsageRated;        // raised after each record is priced
    public event Action<Invoice>? InvoiceGenerated;    // raised when a bill is ready

    // Makes one invoice for one subscription from all the usage records
    public Invoice GenerateInvoice(Subscription subscription, IEnumerable<UsageRecord> records)
    {
        // Can't bill without a subscription
        if (subscription == null) throw new ArgumentNullException(nameof(subscription));

        // The lines of the bill, filled in the loop below
        var lines = new List<RatedLine>();

        // Running total of units used per type, e.g. "Call" -> 140, "Data" -> 900
        // Needed so tiered and free rules know how much is already used up
        var usedSoFar = new Dictionary<string, decimal>();

        // LINQ: keep only THIS customer's records, sorted oldest first,
        // so free minutes and cheap tiers get used up in time order
        var mine = records
            .Where(r => r.MobileNumber == subscription.Customer.MobileNumber)
            .OrderBy(r => r.Timestamp);

        foreach (UsageRecord record in mine)
        {
            // Ask the record which rule prices it. A call gives CallRule, data gives DataRule.
            // Polymorphism: no if/switch on the type here
            IRatingRule rule = record.SelectRule(subscription.Plan);

            // How much of this type was used before this record (0 if it's the first one)
            usedSoFar.TryGetValue(record.UsageType, out decimal before);

            // Ask the rule for the price
            decimal charge = rule.Rate(record, before);

            // Update the running total for this type
            usedSoFar[record.UsageType] = before + record.Units;

            // Make the bill line and add it to the list
            var line = new RatedLine(record, rule.Description, charge);
            lines.Add(line);

            // Announce it. ?.Invoke = only call it if someone has subscribed
            UsageRated?.Invoke(line);
        }

        // Build the invoice. lines.Sum(...) adds up the Charge of every line
        var invoice = new Invoice(subscription, lines, lines.Sum(l => l.Charge));

        // Announce that the bill is ready (the logger and the alert will hear it)
        InvoiceGenerated?.Invoke(invoice);

        // Give the invoice back to whoever called this method
        return invoice;
    }
}
