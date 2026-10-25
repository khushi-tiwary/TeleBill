// Uses classes from Domain and Rating
using TeleBill.Domain;
using TeleBill.Rating;

// This file belongs to the Data group
namespace TeleBill.Data;

// A small box that holds everything the seed creates, so we can return it all together
public record SeedResult(List<TariffPlan> Plans, List<Customer> Customers,
                         List<Subscription> Subscriptions, List<UsageRecord> Usage);

// static class = just a toolbox of methods, you never write "new SeedData()"
public static class SeedData
{
    // Names to build 50 customer names from (10 first names x 5 last names = 50 combos)
    private static readonly string[] FirstNames =
        { "Aarav", "Diya", "Kabir", "Meera", "Rohan", "Ananya", "Vikram", "Isha", "Arjun", "Sneha" };
    private static readonly string[] LastNames =
        { "Sharma", "Iyer", "Reddy", "Nair", "Gupta" };

    // Creates the 3 plans. Each plan picks one rule for calls, SMS and data
    public static List<TariffPlan> CreatePlans() => new()
    {
        new TariffPlan("BASIC99", "Basic 99", 99m,
            callRule: new TieredRateRule(100, 1.00m, 0.60m),   // first 100 min Rs.1, then Rs.0.60
            smsRule:  new FlatRateRule(0.50m),                 // Rs.0.50 per SMS
            dataRule: new FlatRateRule(0.25m)),                // Rs.0.25 per MB
 
        new TariffPlan("SMART299", "Smart 299", 299m,
            callRule: new FreeAllowanceRule(300, 0.50m),       // 300 free minutes, then Rs.0.50
            smsRule:  new FreeAllowanceRule(100, 0.20m),       // 100 free SMS, then Rs.0.20
            dataRule: new TieredRateRule(2048, 0.10m, 0.05m)), // first 2 GB Rs.0.10/MB, then Rs.0.05
 
        new TariffPlan("NIGHT149", "Night Owl 149", 149m,
            callRule: new PeakOffPeakRule(0.80m, 0.20m),       // stretch rule: cheap calls at night
            smsRule:  new FlatRateRule(0.30m),
            dataRule: new PeakOffPeakRule(0.20m, 0.05m)),
    };

    // Builds all the data. Same seed = same data every run
    public static SeedResult Generate(int seed = 42, int customerCount = 50, int usageCount = 1000)
    {
        // Random number maker. Giving it a seed makes it repeat the same numbers every run
        var random = new Random(seed);
        var plans = CreatePlans();
        var customers = new List<Customer>();
        var subscriptions = new List<Subscription>();

        // Make 50 customers
        for (int i = 0; i < customerCount; i++)
        {
            // i % 10 picks the first name, i / 10 picks the last name
            string name = FirstNames[i % FirstNames.Length] + " " + LastNames[i / FirstNames.Length % LastNames.Length];

            // D5 = pad to 5 digits, so numbers go 9845000000, 9845000001, ...
            var customer = new Customer("98450" + i.ToString("D5"), name);
            customers.Add(customer);

            // Put the customer on a random plan from 1 Oct 2026
            subscriptions.Add(new Subscription(customer, plans[random.Next(plans.Count)], new DateOnly(2026, 10, 1)));
        }

        // The last 4 customers get NO usage, so the "no usage" report has something to show
        int activeCustomers = customerCount - 4;
        var usage = new List<UsageRecord>();
        var monthStart = new DateTime(2026, 10, 1);

        // Make 1000 usage records
        for (int i = 0; i < usageCount; i++)
        {
            // Pick a random active customer
            string mobile = customers[random.Next(activeCustomers)].MobileNumber;

            // Pick a random minute in October (31 days x 24 hours x 60 minutes)
            DateTime when = monthStart.AddMinutes(random.Next(31 * 24 * 60));

            // random.Next(3) gives 0, 1 or 2. The switch picks the record type from it
            UsageRecord record = random.Next(3) switch
            {
                0 => new CallRecord(mobile, when, random.Next(1, 61)),         // call of 1-60 minutes
                1 => new SmsRecord(mobile, when, random.Next(1, 21)),          // 1-20 SMS
                _ => new DataRecord(mobile, when, random.Next(10, 1501))       // 10-1500 MB (_ = anything else)
            };
            usage.Add(record);
        }

        // Send everything back in one box
        return new SeedResult(plans, customers, subscriptions, usage);
    }
}
