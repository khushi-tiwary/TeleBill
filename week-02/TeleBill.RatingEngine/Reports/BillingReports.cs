// Uses classes from Domain and Rating
using TeleBill.Domain;
using TeleBill.Rating;

// This file belongs to the Reports group
namespace TeleBill.Reports;

// The four LINQ reports. static = just a toolbox, no "new" needed
public static class BillingReports
{
    // REPORT 1: top 5 customers by bill amount
    public static void TopCustomersBySpend(List<Invoice> invoices, int count = 5)
    {
        Console.WriteLine($"\nREPORT 1: Top {count} customers by spend");

        var top = invoices
            .OrderByDescending(i => i.Total)   // biggest bill first
            .Take(count)                       // keep only the first 5
                                               // (inv, index) = the invoice and its position (0, 1, 2...). +1 gives rank 1, 2, 3...
            .Select((inv, index) => new { Rank = index + 1, inv.Subscription.Customer.Name, Plan = inv.Subscription.Plan.Code, inv.Total });

        foreach (var row in top)
            Console.WriteLine($"   {row.Rank}. {row.Name,-16} {row.Plan,-9} Rs.{row.Total,9:N2}");
    }

    // REPORT 2: total usage per type (Call / Sms / Data)
    public static void UsageByType(List<Invoice> invoices)
    {
        Console.WriteLine("\nREPORT 2: Total usage by type");

        var byType = invoices
            .SelectMany(i => i.Lines)                 // flatten: all lines from all invoices in one list
            .GroupBy(l => l.Record.UsageType)         // make 3 groups: Call, Sms, Data
            .Select(g => new
            {
                Type = g.Key,                         // the group name, e.g. "Call"
                Records = g.Count(),                  // how many records in this group
                Units = g.Sum(l => l.Record.Units),   // total minutes / SMS / MB
                Unit = g.First().Record.Unit,         // "min", "sms" or "MB"
                Charges = g.Sum(l => l.Charge)        // total money for this type
            })
            .OrderByDescending(x => x.Charges);       // highest earning type first

        foreach (var t in byType)
            Console.WriteLine($"   {t.Type,-5} {t.Records,4} records  {t.Units,9:N0} {t.Unit,-3}  Rs.{t.Charges,10:N2}");
    }

    // REPORT 3: customers who used nothing this month
    public static void CustomersWithNoUsage(List<Customer> customers, List<UsageRecord> usage)
    {
        Console.WriteLine("\nREPORT 3: Customers with no usage");

        // Keep a customer only if NO usage record has their mobile number
        var idle = customers
            .Where(c => !usage.Any(u => u.MobileNumber == c.MobileNumber))
            .ToList();

        if (idle.Count == 0)
            Console.WriteLine("   (none)");
        foreach (var c in idle)
            Console.WriteLine($"   {c}");   // uses Customer.ToString(): "Name (number)"
    }

    // REPORT 4: average bill for each plan
    public static void AverageBillPerPlan(List<Invoice> invoices)
    {
        Console.WriteLine("\nREPORT 4: Average bill per plan");

        var perPlan = invoices
            .GroupBy(i => i.Subscription.Plan.Code)   // one group per plan
            .Select(g => new { Plan = g.Key, Customers = g.Count(), Average = g.Average(i => i.Total) })
            .OrderBy(x => x.Plan);                    // alphabetical by plan code

        foreach (var p in perPlan)
            Console.WriteLine($"   {p.Plan,-9} {p.Customers,2} customers  average Rs.{p.Average,9:N2}");
    }
}
