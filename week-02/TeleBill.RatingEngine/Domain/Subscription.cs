namespace Telebill.Domain;

public class Subscription
{
    public Customer customer {get; }//customer who wons this subscription. Read only
    public TariffPlan Plan {get; }// the plan the customer is on
    public DateOnly StartDate {get; } // the day the subscription started (DateOnly is just a date, no time)

// constructor runs when we write "new Subscription(...)"
public Subscription(Customer customer, TariffPlan Plan, DateOnly StartDate){

    //Save the customer. If it's null, stop and throwan error naming "customer"
    Customer= customer ?? throw new ArgumentNullException(nameof(customer));

    //Save the plan. If it's null, stop and throwan error naming "plan"
    Plan = plan ?? throw new ArgumentNullException(nameof(plan));

   // save the start date. No check needed because a DateOnly can never be null
    StartDate = startDate;
}
}