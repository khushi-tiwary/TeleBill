namespace TeleBill.Domain; // puts this class in the "Domain" group, matching its folder

public class Customer //customer of the telecom company,invalid customer cant exist
{
public string MobileNumber {get; }// unique id of the customer never changes 
public string Name {get; }

//constructor checks the number and name first
public Customer (string mobileNumber, string name){
    if(!IsValidMobile(mobileNumber))
    throw new ArgumentException($"Invalid mobile number: {mobileNumber}");// If either is bad, it throws, and the customer is never created.
    if(string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Customer name is requires");

    MobileNumber = mobileNumber;
    Name=name.Trim();
}

public static bool IsValidMobile(string? mobile)
//static beacuse it doesn't need a customer to work. We'll reuse it later for usage records
{
    if(mobile == null || mobile.Length!=10 || mobile[0] < '6')
    return false;
    foreach(char c in mobile){
        if(c<'0' || c>'9')
        return false;

    }
    return true;
}

public override string ToString()=> $"{Name} ({MobileNumber})"; // sets how a cudtomer prints, eg. Khushi (9845000000).
}