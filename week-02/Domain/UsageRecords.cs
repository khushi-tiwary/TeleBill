
using TeleBill.Rating;
 
// This file belongs to the Domain group
namespace TeleBill.Domain;
 
//  PARENT CLASS
// abstract = you can't do "new UsageRecord(...)". You can only create a Call, Sms or Data record.
public abstract class UsageRecord
{
    // Which customer used it. Read-only.
    public string MobileNumber { get; }
 
    // When it happened (date + time). Needed for peak/off-peak pricing.
    public DateTime Timestamp { get; }
 
    // protected = only child classes can call this constructor
    protected UsageRecord(string mobileNumber, DateTime timestamp)
    {
        // Reuse the check from Customer.cs, so a record can't have a bad number
        if (!Customer.IsValidMobile(mobileNumber))
            throw new ArgumentException($"Invalid mobile number: {mobileNumber}");
 
        // Save the values
        MobileNumber = mobileNumber;
        Timestamp = timestamp;
    }
 
    // abstract property = no value here, every child MUST give its own
    public abstract string UsageType { get; }   // "Call", "Sms" or "Data"
    public abstract decimal Units { get; }      // how much: minutes, messages or MB
    public abstract string Unit { get; }        // the unit's name: "min", "sms" or "MB"
 
    // Every child must say which rule from the plan prices it.
    // This is why the engine needs NO if/switch on the record type.
    public abstract IRatingRule SelectRule(TariffPlan plan);
}
 
// ": UsageRecord" = CallRecord inherits from UsageRecord (it IS a UsageRecord)
public class CallRecord : UsageRecord
{
    // How long the call was
    public int DurationMinutes { get; }
 
    // ": base(...)" sends the number and time up to the parent's constructor first
    public CallRecord(string mobileNumber, DateTime timestamp, int durationMinutes)
        : base(mobileNumber, timestamp)
    {
        // A call of 0 or negative minutes makes no sense
        if (durationMinutes <= 0)
            throw new ArgumentException("Call duration must be more than 0 minutes");
 
        DurationMinutes = durationMinutes;
    }
 
    // override = fill in the parent's abstract parts. "=>" is a short way to return a value.
    public override string UsageType => "Call";
    public override decimal Units => DurationMinutes;// a call is measured in minutes
    public override string Unit => "min";
 
    // A call is priced by the plan's call rule
    public override IRatingRule SelectRule(TariffPlan plan) => plan.CallRule;
}
 
public class SmsRecord : UsageRecord
{
    // How many messages were sent
    public int MessageCount { get; }
 
    public SmsRecord(string mobileNumber, DateTime timestamp, int messageCount)
        : base(mobileNumber, timestamp)
    {
        // Must send at least 1 message
        if (messageCount <= 0)
            throw new ArgumentException("SMS count must be more than 0");
 
        MessageCount = messageCount;
    }
 
    public override string UsageType => "Sms";
    public override decimal Units => MessageCount;// SMS is measured in messages
    public override string Unit => "sms";
 
    // SMS is priced by the plan's SMS rule
    public override IRatingRule SelectRule(TariffPlan plan) => plan.SmsRule;
}
 
public class DataRecord : UsageRecord
{
    // How much data was used. decimal because it can be 12.5 MB.
    public decimal Megabytes { get; }
 
    public DataRecord(string mobileNumber, DateTime timestamp, decimal megabytes)
        : base(mobileNumber, timestamp)
    {
        // Must use more than 0 MB
        if (megabytes <= 0)
            throw new ArgumentException("Data used must be more than 0 MB");
 
        Megabytes = megabytes;
    }
 
    public override string UsageType => "Data";
    public override decimal Units => Megabytes; // data is measured in MB
    public override string Unit => "MB";
 
    // Data is priced by the plan's data rule
    public override IRatingRule SelectRule(TariffPlan plan) => plan.DataRule;
}