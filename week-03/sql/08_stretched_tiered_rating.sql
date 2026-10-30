/* 08_stretch_tiered_rating.sql - Stretch goal
   Tiered call rating in ONE set-based statement using window functions.
   First 300 minutes per subscription per month at plan rate, the rest at 50%.
   No cursor, no loop. */

USE TeleBillDB;
GO
 
DECLARE @Month DATE = '2026-10-01', @TierLimit DECIMAL(12,2) = 300;
 
WITH Calls AS (
    SELECT u.SubscriptionId,
           u.Quantity,
           r.RatePerUnit,
           SUM(u.Quantity) OVER (PARTITION BY u.SubscriptionId  -- running total per subscription
                                 ORDER BY u.UsageStart, u.UsageId
                                 ROWS UNBOUNDED PRECEDING) AS RunningMinutes
    FROM dbo.UsageRecords u
    JOIN dbo.Subscriptions s ON s.SubscriptionId = u.SubscriptionId
    CROSS APPLY dbo.fn_GetApplicableRate(s.PlanId, u.UsageTypeId, CAST(u.UsageStart AS DATE)) r
    WHERE u.UsageTypeId = 1
      AND u.UsageStart >= @Month AND u.UsageStart < DATEADD(MONTH, 1, @Month)
),
Split AS (
    SELECT SubscriptionId, Quantity, RatePerUnit,
           CASE WHEN RunningMinutes <= @TierLimit THEN Quantity  -- fully in tier 1
                WHEN RunningMinutes - Quantity >= @TierLimit THEN 0 -- fully in tier 2
                ELSE @TierLimit - (RunningMinutes - Quantity) -- crosses the line
           END AS Tier1Minutes
    FROM Calls
)
SELECT SubscriptionId,
       SUM(Quantity) AS TotalMinutes,
       SUM(Tier1Minutes) AS Tier1Minutes,
       SUM(Quantity - Tier1Minutes) AS Tier2Minutes,
       ROUND(SUM(Tier1Minutes * RatePerUnit
               + (Quantity - Tier1Minutes) * RatePerUnit * 0.5), 2) AS TieredCharge,
       ROUND(SUM(Quantity * RatePerUnit), 2)  AS FlatCharge
FROM Split
GROUP BY SubscriptionId
ORDER BY SubscriptionId;
GO
 