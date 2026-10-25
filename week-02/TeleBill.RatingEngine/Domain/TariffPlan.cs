// a phone plan- it has a code, a name, a monthly rental, and three pricing rules: one for calls, one for SMS, one for data.

using TeleBill.Rating; //lets this file use IRatingRule, which lives in the Rating folder.
namespace TeleBill.Domain;

public class TariffPlan 
{
public string Code {get; }
public string Name {get; }
public decimal MonthlyRental {get; }
//IRating rule is an interface, it's a promise:"I can price a usage." 
//The plan doesnt care which kind of rule it gets (flat, tiered, free minutes), only that it can price
public IRatingRule CallRule {get; }
public IRatingRule SmsRule {get; }
public IRatingRule DataRule {get; }

public TariffPlan (string code, string name, decimal monthlyRental, IRatingRule callRule, 
IRatingRule smsRule, IRatingRule dataRule)
{
if(string.IsNullOrWhiteSpace(code))
throw new ArgumentException("Plan code is required");
if(string.IsNullOrWhiteSpace(name))
throw new ArgumentException("Plan name is required");
if(monthlyRental < 0)
throw new ArgumentException("Monthly rentalcannot be negative");

Code= code.Trim().ToUpper(); // cleans the input "basic99" to "BASIC99"
Name=name.Trim();
MonthlyRental = monthlyRental;
//??-why is this used- null-coalescing operator- use the left side, but if it's null, use the right side instead
CallRule=callRule ?? throw new ArgumentException(nameof(callRule));
//means-use callRule, but if it's null, throw an error. A plan must have all three rules
SmsRule=smsRule ?? throw new ArgumentException(nameof(smsRule));// nameof turns a variable's name into text
DataRule=dataRule ?? throw new ArgumentException(nameof(dataRule));
}
public override string ToString()=> $"{Code} {Name}";
}
