// Bring in all our folders' classes
using TeleBill.Data;
using TeleBill.Notifications;
using TeleBill.Rating;
using TeleBill.Reports;

Console.WriteLine("TELEBILL RATING ENGINE v1 - October 2026 billing run");

// 1. Make the test data (fixed seed 42, so every run gives the same result)
SeedResult data = SeedData.Generate(seed: 42);
Console.WriteLine($"Loaded {data.Plans.Count} plans, {data.Customers.Count} customers, {data.Usage.Count} usage records\n");

// Print the plans and their rules
Console.WriteLine("Plans:");
foreach (var plan in data.Plans)
{
    Console.WriteLine($"   {plan} - rental Rs.{plan.MonthlyRental}");
    Console.WriteLine($"      call: {plan.CallRule.Description} | sms: {plan.SmsRule.Description} | data: {plan.DataRule.Description}");
}

// 2. Create the engine and the two listeners
var engine = new RatingEngine();
var logger = new ConsoleLogger();
var alert = new HighBillAlert(threshold: 1500m);   // alert for bills above Rs.1500

// Subscribe both listeners to "invoice ready". += adds a listener
engine.InvoiceGenerated += logger.OnInvoiceGenerated;
engine.InvoiceGenerated += alert.OnInvoiceGenerated;

// 3. Detailed run for the first customer: show every priced line too
var first = data.Subscriptions[0];
Console.WriteLine($"\nDETAILED RATING: {first.Customer} on {first.Plan}");
engine.UsageRated += logger.OnUsageRated;          // subscribe: print every line
engine.GenerateInvoice(first, data.Usage);
engine.UsageRated -= logger.OnUsageRated;          // unsubscribe (-=), so the full run isn't 1000 lines long

// 4. Bill every customer. Select runs GenerateInvoice for each subscription
Console.WriteLine("\nBILLING RUN: all customers");
var invoices = data.Subscriptions
    .Select(sub => engine.GenerateInvoice(sub, data.Usage))
    .ToList();   // ToList() makes it run now (LINQ waits until you ask for results)

Console.WriteLine($"\n{invoices.Count} invoices generated, total billed Rs.{invoices.Sum(i => i.Total):N2}, {alert.AlertsRaised} high-bill alerts");

// 5. Print the four reports
BillingReports.TopCustomersBySpend(invoices);
BillingReports.UsageByType(invoices);
BillingReports.CustomersWithNoUsage(data.Customers, data.Usage);
BillingReports.AverageBillPerPlan(invoices);
