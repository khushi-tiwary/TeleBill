// Lets this file use RatedLine and Invoice (Rating folder)
using TeleBill.Rating;

// This file belongs to the Notifications group
namespace TeleBill.Notifications;

// Listener 1: prints what the engine does to the console
public class ConsoleLogger
{
    // Runs every time the engine raises UsageRated (one record priced)
    public void OnUsageRated(RatedLine line)
    {
        // r = the usage record behind this line (short name, easier to read)
        var r = line.Record;

        // dd-MMM HH:mm = date like "05-Oct 14:30". ,-4 and ,7 = column widths so things line up
        Console.WriteLine($"   [RATED] {r.Timestamp:dd-MMM HH:mm} {r.UsageType,-4} {r.Units,7} {r.Unit,-3} -> Rs.{line.Charge,7:N2}  ({line.RuleUsed})");
    }

    // Runs every time the engine raises InvoiceGenerated (one bill ready)
    public void OnInvoiceGenerated(Invoice invoice)
    {
        // N2 = number with 2 decimals and commas, e.g. 1,234.50
        Console.WriteLine($"   [INVOICE] {invoice.Subscription.Customer.Name,-16} {invoice.Subscription.Plan.Code,-9} " +
                          $"{invoice.Lines.Count,3} lines  usage Rs.{invoice.UsageCharges,8:N2} + rental Rs.{invoice.Rental,6:N2} = Rs.{invoice.Total,8:N2}");
    }
}

// Listener 2: warns when a bill is above a limit
public class HighBillAlert
{
    // The limit, e.g. 1500. Set once when the alert is created (configurable)
    public decimal Threshold { get; }

    // How many alerts were raised. "private set" = only this class can change it
    public int AlertsRaised { get; private set; }

    public HighBillAlert(decimal threshold)
    {
        // A limit of 0 or less makes no sense
        if (threshold <= 0) throw new ArgumentException("Threshold must be more than 0");
        Threshold = threshold;
    }

    // Runs every time a bill is ready
    public void OnInvoiceGenerated(Invoice invoice)
    {
        // Bill is within the limit, so do nothing and leave the method
        if (invoice.Total <= Threshold) return;

        // Bill is too high: count it and print a warning
        AlertsRaised++;
        Console.WriteLine($"   [ALERT] High bill: {invoice.Subscription.Customer.Name} Rs.{invoice.Total:N2} (limit Rs.{Threshold:N2})");
    }
}
