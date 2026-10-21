 
using System; // Imports basic C# functionality, including Console and DateTime.
using System.Collections.Generic; // Imports Dictionary and List collections.
using System.Linq; // Imports LINQ methods such as Where and ToList.
 
//class to define all triff rates in one place
class TariffRates // Stores all tariff rates in one class so they can be changed easily.
{
    public const decimal first100callmins = 1.00m; // first 100 calls= Rs.1; m indicates a decimal literal.
    public const decimal extracallmins = 0.50m; //extra calls after 100= Rs.0.50
    public const decimal smsrate = 0.20m; // sms cost= Rs.0.20
    public const decimal dataRatePerMB = 0.05m; // each mb of data cost= Rs.0.05
}
 
//Defines info stored for one customer
class Customer // Represents one prepaid mobile customer.
{
    public string Name { get; set; } // stores customer's name, get-reads it, set-changes it
    public string MobileNumber { get; set; } // customer's mobile number
    public decimal Balance { get; set; } // customer's current prepaid balance
    public int totalcallmins { get; set; } // customer's total call limits
 
    //constructor runs when a new customer object is created
    public Customer(string name, string mobileNumber) // Receives the new customer's name and mobile number.
    {
        Name = name; // assigns the supplied name to this customer's name property
        MobileNumber = mobileNumber; // same for mobile number
        Balance = 0m; // startss the customer's prepaid balance at 0
        totalcallmins = 0; //starts the total call duration at 0
    }
}
 
class UsageRecord // Represents one call, SMS, or data transaction.
{
    public string MobileNumber { get; set; } // identifies the customer who used the service
    public string UsageType { get; set; } //stores the type of usage
    public decimal Quantity { get; set; } // stores mins, sms count or data consumed in mb
    public decimal Charge { get; set; } // amount charged for this usage
    public DateTime Date { get; set; } // stores data and time when the record was created
 
    // Constructor initializes a new usage record with its transaction details.
    public UsageRecord(string mobileNumber, string usageType, decimal quantity, decimal charge)
    {
        MobileNumber = mobileNumber; // recieves the customer's mobile number
        UsageType = usageType; // Stores whether this transaction is Call, SMS, or Data.
        Quantity = quantity; // Stores the amount of service consumed.
        Charge = charge; // Stores the amount charged for this transaction.
        Date = DateTime.Now; // Saves the current date and time.
    }
}
 
// defines bundle or a a pacakage that can contain other bundles
class Bundle // Represents a package that may contain nested packages.
{
    public string Name { get; set; } // stores bundle's name
    public decimal Price { get; set; } // Stores the price of this bundle alone.
    public List<Bundle> SubBundles { get; set; } // Understand this- stores the bundles nested inside this bundle
 
    // constructor recieves the bundle's name and price
    public Bundle(string name, decimal price) // Runs when a new bundle is created.
    {
        Name = name; // saves name and ppric
        Price = price; // Saves the price provided when creating the bundle.
        SubBundles = new List<Bundle>(); // What does this do???--empty list for nested bundles
    }
}
 
//Application logic is here
class Program // Contains the main application logic and menu.
{
    static Dictionary<string, Customer> Customers = new(); // stores customers using their mobile numbers as unique keys
    static List<UsageRecord> UsageHistory = new(); // stores every recorded usage transaction
 
    static void Main() // entry point where application statrt
    {
        bool running = true; // controls whether the application menu continues to run
 
        while (running) // Repeats the menu until the user chooses Exit.
        {
            Console.WriteLine("\nPREPAID RECHARGE AND TARIFF CALCULATOR"); // Displays the application title.
            Console.WriteLine("1. Add Customer"); // Option for registering a customer.
            Console.WriteLine("2. Recharge"); // Option for adding money to a balance.
            Console.WriteLine("3. Record Call Usage"); // Option for recording a call.
            Console.WriteLine("4. Record SMS Usage"); // Option for recording SMS messages.
            Console.WriteLine("5. Record Data Usage"); // Option for recording data consumption.
            Console.WriteLine("6. View Balance"); // Option for checking the balance.
            Console.WriteLine("7. View Usage Summary"); // Option for viewing usage records.
            Console.WriteLine("8. Calculate Bundle Price"); // Option for calculating nested bundle prices.
            Console.WriteLine("9. Exit"); // Option for closing the application.
 
            int choice = ReadInt("Enter your choice: ", 1, 9); // reads and validates a menu choice between 1 and 9
 
            //Switch case/ Menu-driven
            switch (choice) // Executes the case matching the user's selection.
            {
                case 1: // Runs when the user selects Add Customer.
                    AddCustomer(); // method call that creates a customer
                    break; // Exits the switch case.
 
                case 2: // Runs when the user selects Recharge.
                    Recharge(); // method call that adds money to a balance
                    break; // switch statement exit
 
                case 3: // Runs when the user selects Record Call Usage.
                    RecordCallUsage(); // Calls the method that records a call.
                    break; // Exits this case.
 
                case 4: // Runs when the user selects Record SMS Usage.
                    RecordSMSUsage(); // Calls the method that records SMS usage.
                    break; // Exits this case.
 
                case 5: // Runs when the user selects Record Data Usage.
                    RecordDataUsage(); // Calls the method that records data usage.
                    break; // Exits this case.
 
                case 6: // Runs when the user selects View Balance.
                    CheckBalance(); // Calls the method that displays a customer's balance.
                    break; // Exits this case.
 
                case 7: // Runs when the user selects View Usage Summary.
                    ViewUsageSummary(); // Calls the method that displays usage history.
                    break; // Exits this case.
 
                case 8: // Runs when the user selects Calculate Bundle Price.
                    CalculateBundlePrice(); // calculating a nested bundle price
                    break; // Exits this case.
 
                case 9: // Runs when the user selects Exit.
                    running = false; // Changes the condition so the menu stops repeating.
                    Console.WriteLine("Thankyou for using the application"); // Displays the exit message.
                    break; // Exits this case.
            }
        }
    }
 
    //Method that adds a new customer
    static void AddCustomer()
    {
        Console.WriteLine("\nADD CUSTOMER"); // Displays the section heading.
 
        string name = ReadRequiredText("Enter customer name: "); // reads a name and rejects empty input
        string mobile = ReadMobileNumber("Enter 10-digit mobile number: "); //reading and validating a 10-digit mobile number
 
        if (Customers.ContainsKey(mobile)) //checks if this mobile num already exists in the dictionary, mobile number is the key
        {
            Console.WriteLine("A customer with this mobile number already exists."); // Explains why registration was rejected.
            return; // Stops this method without creating a duplicate customer.
        }
 
        Customer customer = new Customer(name, mobile); // new customer object with supplied details
        Customers.Add(mobile, customer); // adds the customer using the mobile number as the dictionary key
        Console.WriteLine("Customer added successfully!"); // Confirms that registration succeeded.
    }
 
    static void Recharge() // Adds money to an existing customer's account.
    {
        Console.WriteLine("\nRECHARGE"); // Displays the section heading.
 
        Customer? customer = FindCustomer(); //searches for the customer by mobile id. also why did we use "?"
 
        if (customer == null) //customer not found
            return; // stops recharge operation
 
        decimal amount = ReadDecimal("Enter a recharge amount: ", 0.01m, decimal.MaxValue); // Reads a recharge amount greater than zero.
        customer.Balance += amount; //adds recharge amount to the existing balance
 
        Console.WriteLine($"Recharge successful! New balance: Rs {customer.Balance:F2}"); // Displays the updated balance to two decimal places.
        //displays the updated balance to two decimal places
    }
 
    static void RecordCallUsage() // Records call minutes and deducts their charge.
    {
        Console.WriteLine("\nRECORD CALL USAGE"); // Displays the section heading.
 
        Customer? customer = FindCustomer(); //find customer using their mobile number
 
        if (customer == null) // Checks whether the customer was found.
            return; // Stops if the customer does not exist.
 
        int minutes = ReadInt("Enter call duration in minutes: ", 1, int.MaxValue); // reads a positive number of call minutes
 
        int oldMins = customer.totalcallmins; // Saves the customer's previous total call minutes.
        int newMins = oldMins + minutes; // Calculates the total minutes after this call.
        decimal oldCharge = CalculateCallCharge(oldMins); // Calculates the charge for the previous total minutes.
 
        decimal newCharge = CalculateCallCharge(newMins); // Calculates the charge for the new total minutes.
        decimal charge = newCharge - oldCharge; // calculates only the additional charge caused by this call
 
        if (customer.Balance < charge) // Checks whether the customer can afford the call.
        {
            Console.WriteLine("Insufficient balance. Please recharge first."); // Displays an insufficient balance message.
            return; // Stops without changing the balance or usage history.
        }
 
        customer.Balance -= charge; // Deducts the call charge from the balance.
        customer.totalcallmins = newMins; //new accumulated call duration
 
        UsageHistory.Add(new UsageRecord(customer.MobileNumber, "Call", minutes, charge)); // Saves the call transaction.
        // creates a usage record and adds it to the history, supplies the mobile numbers, usgae type, duration and charge
 
        Console.WriteLine($"Call charge: Rs {charge:F2}"); // charge for this call
        Console.WriteLine($"Remaining balance: Rs {customer.Balance:F2}"); // Displays the balance after the call.
    }
 
    static decimal CalculateCallCharge(int totalMins) // Calculates the total call charge for accumulated minutes.
    {
        int firstMins = Math.Min(totalMins, 100); // uses the smaller value; TOTALMINUTES OR 100
        int extraMins = Math.Max(0, totalMins - 100); // calculates mins above 100, never allowing a negative r
 
return firstMins * TariffRates.first100callmins + extraMins * TariffRates.extracallmins; // charge for the first slab + charge for mins in the second slab
    }
 
    static void RecordSMSUsage() // Records SMS usage and deducts its charge.
    {
        Console.WriteLine("\nRECORD SMS USAGE"); // Displays the section heading.
 
        Customer? customer = FindCustomer(); // Finds the customer by mobile number.
 
        if (customer == null) // Checks whether the customer exists.
            return; // Stops if the customer does not exist.
 
        int count = ReadInt("Enter number of SMS messages: ", 1, int.MaxValue); //reads +ve sms count
 
        decimal charge = count * TariffRates.smsrate; // caclculates the total sms charge
 
        if (customer.Balance < charge) // Checks whether the customer can afford the SMS messages.
        {
            Console.WriteLine("Insufficient balance. Please recharge first."); // Displays an insufficient balance message.
            return; // Stops without deducting money or recording usage.
        }
 
        customer.Balance -= charge; // Deducts the SMS charge.
 
        UsageHistory.Add(new UsageRecord(customer.MobileNumber, "SMS", count, charge)); // Saves the SMS transaction.
 // creates a usage record and adds it to the history, supplies the mobile numbers, usgae type, duration and charge
 
        Console.WriteLine($"SMS charge: Rs {charge:F2}"); // Displays the SMS Charge.
        Console.WriteLine($"Remaining balance: Rs {customer.Balance:F2}"); // Displays the remaining balance.
    }
 
    static void RecordDataUsage() // Records data consumption and deducts its charge.
    {
        Console.WriteLine("\nRECORD DATA USAGE"); // Displays the section heading.
 
        Customer? customer = FindCustomer(); // Finds the customer by mobile number.
 
        if (customer == null) // Checks whether the customer exists.
            return; // Stops if the customer does not exist.
 
        decimal dataMB = ReadDecimal("Enter data usage in MB: ", 0.01m, decimal.MaxValue); // Reads a positive data amount.
        decimal charge = dataMB * TariffRates.dataRatePerMB; // Calculates the data charge.
 
        if (customer.Balance < charge) // Checks whether the customer can afford the data usage.
        {
            Console.WriteLine("Insufficient balance. Please recharge first."); // Displays an insufficient balance message.
            return; // Stops without deducting money or recording usage.
        }

        customer.Balance -= charge; // Deducts the data charge.
 
        UsageHistory.Add(new UsageRecord(customer.MobileNumber, "Data", dataMB, charge)); // Saves the data transaction.
        // creates a usage record and adds it to the history, supplies the mobile numbers, usgae type, duration and charge
 
        Console.WriteLine($"Data charge: Rs {charge:F2}"); // Displays the data charge.
        Console.WriteLine($"Remaining balance: Rs {customer.Balance:F2}"); // Displays the remaining balance.
    }
 
    static void CheckBalance() // Displays a customer's current account details.
    {
        Customer? customer = FindCustomer(); // Looks up the customer.
 
        if (customer == null) // Checks whether the customer was found.
            return; // Stops if the customer was not found.
 
        Console.WriteLine($"\nCustomer: {customer.Name}"); // Displays the customer's name.
        Console.WriteLine($"Mobile: {customer.MobileNumber}"); // Displays the customer's mobile number.
        Console.WriteLine($"Balance: Rs {customer.Balance:F2}"); // Displays the current balance to two decimal places.
    }
    // Displays usage records and total charges for a customer.
    static void ViewUsageSummary()
    {
        Customer? customer = FindCustomer(); // Finds the customer whose history should be displayed.
 
        if (customer == null) // Checks whether the customer was found.
            return; // Stops if the customer does not exist.
 
        List<UsageRecord> records = UsageHistory // Filters the history to include only this customer's records.
            .Where(r => r.MobileNumber == customer.MobileNumber) // where checks the condition
            .ToList(); //stores the matching records in a list
 
        Console.WriteLine($"\nUSAGE SUMMARY : {customer.Name}"); // Displays the customer's name in the summary heading.
 
        if (records.Count == 0) // checks whether any usage records were found
        {
            Console.WriteLine("No usage records found"); // Tells the user there are no transactions.
            return; // Stops because there are no records to summarize.
        }
 
        decimal totalCharges = 0m; // Starts the total of all recorded charges at zero.

        foreach (UsageRecord record in records) // Iterates through each transaction belonging to this customer.
        {
            Console.WriteLine(
                $"{record.Date:dd-MM-yyyy HH:mm:ss} | " + // Displays the transaction date and time.
                $"{record.UsageType} | " + // Displays whether the usage was Call, SMS, or Data.
                $"Quantity: {record.Quantity} | " + // Displays the quantity consumed.
                $"Charge: Rs {record.Charge:F2}"); // Displays the transaction charge.
 
            totalCharges += record.Charge; // adds the current record's charge to the running total
        }
 
        Console.WriteLine($"Total usage charges: Rs {totalCharges:F2}"); // Displays the sum of recorded usage charges.
        Console.WriteLine($"Current balance: Rs {customer.Balance:F2}"); // Displays the current prepaid balance.
    }
 
    // Demonstrates recursive calculation of nested bundle prices.
    static void CalculateBundlePrice()
    {
        Bundle baseBundle = new Bundle("Base Plan", 99m); // creates the main bundles with its own base price
        Bundle dataBundle = new Bundle("Data Add-on", 49m); // creates a data add-on bundle
        Bundle smsBundle = new Bundle("SMS Add-on", 19m); // Creates an SMS add-on bundle.
        Bundle premiumBundle = new Bundle("Premium Pack", 29m); // creates another bundle that will be nested inside the data bundle

        // Nest smaller bundles inside larger bundles
        dataBundle.SubBundles.Add(premiumBundle); // Adds the premium pack inside the data add-on.
        baseBundle.SubBundles.Add(dataBundle); // Adds the data add-on inside the base plan.
        baseBundle.SubBundles.Add(smsBundle); // Adds the SMS add-on inside the base plan.
 
        decimal totalPrice = CalculateTotalBundlePrice(baseBundle); // calling recursive method to calculate the entire bundle price
 
        Console.WriteLine("\nBUNDLE PRICE"); // Displays the bundle section heading.
        Console.WriteLine($"Bundle: {baseBundle.Name}"); // Displays the top-level bundle name.
        Console.WriteLine($"Total bundle price: Rs {totalPrice:F2}"); // Displays the price of the bundle and all nested bundles.
    }
 
    // Recursively adds the price of a bundle and all its child bundles.
    static decimal CalculateTotalBundlePrice(Bundle bundle)
    {
        decimal total = bundle.Price; // current bundle's own price
 
        foreach (Bundle child in bundle.SubBundles) // Visits every bundle nested inside the current bundle.
        {
            total += CalculateTotalBundlePrice(child); // calls same methid for each child and adds its total
        }

    return total; // Returns the combined price of this bundle and all its nested bundles.
    }
 
    // Finds a customer using their mobile number.
    static Customer? FindCustomer()
    {
        string mobile = ReadMobileNumber("Enter 10-digit mobile number: "); // Reads and validates the mobile number.
 
        if (Customers.TryGetValue(mobile, out Customer? customer)) // TryGet-checks the dictionary and retrieves customer if found
        {
            return customer; // Returns the customer if the mobile number exists.
        }
 
        Console.WriteLine("Customer not found. Please add the customer first."); // Explains why the requested action cannot continue.
        return null; // Indicates that no customer was found.
    }
 
    // Reads an integer within a specified range.
    static int ReadInt(string prompt, int min, int max) //reads an integer within a specifies range
    {
        while (true) // Repeats until the user enters a valid integer.
        {
            Console.Write(prompt); // Displays the input prompt.
 
            string? input = Console.ReadLine(); // Reads the user's input as text.
 
            if (int.TryParse(input, out int value) && value >= min && value <= max) // TryParse converts text to an integer if possible
            {
                return value; // Returns the number if it is within the allowed range.
            }
 
            Console.WriteLine($"Invalid input. Enter a number between {min} and {max}."); // Explains the valid range.
        }
    }
 
    // Reads a decimal value within a specified range.
    static decimal ReadDecimal(string prompt, decimal min, decimal max)
    {
        while (true) // Repeats until the user enters a valid decimal.
        {
            Console.Write(prompt); // Displays the input prompt.

            string? input = Console.ReadLine(); // Reads the user's input as text.
 
            if (decimal.TryParse(input, out decimal value) && value >= min && value <= max)
            // vaidating the decimal format, and the conditions validates its range
            {
                return value; // Returns the decimal if it is within the allowed range.
            }
 
            Console.WriteLine($"Invalid input. Enter a value between {min} and {max}."); // Explains the allowed range.
        }
    }
 
    // Prevents empty or whitespace only text.
    static string ReadRequiredText(string prompt)
    {
        while (true)// repeats until non empty text is provided
        {
            Console.Write(prompt); // Displays the input prompt.
 
            string? input = Console.ReadLine(); // Reads the user's text.
 
            if (!string.IsNullOrWhiteSpace(input)) // checks whether the input is not null, epty or whitespace only
            {
                return input.Trim(); // removes space from the beginning and end
            }
 
            Console.WriteLine("Input cannot be empty."); // Asks the user to provide non-empty text.
        }
    }
 
    // Validates a 10-digit mobile number.
    static string ReadMobileNumber(string prompt)
    {
        while (true) // Repeats until a valid mobile number is entered.
        {
            Console.Write(prompt); // enter mobile number
 
            string? mobile = Console.ReadLine(); // Reads the mobile number as text.
 
            if (!string.IsNullOrWhiteSpace(mobile) && mobile.Length == 10 && mobile.All(char.IsDigit))
            // checks that the value is not empty and has 10 characters which are only digits
            {
                return mobile; // Returns the validated mobile number.
            }
 
            Console.WriteLine(
                "Invalid mobile number. Enter exactly 10 digits."); // Explains the required mobile number format.
        }
    }
}