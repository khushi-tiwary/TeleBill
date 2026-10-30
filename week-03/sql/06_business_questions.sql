/* 06_business_questions.sql - Task 4
   20 business questions using joins, aggregation, subqueries and CTEs.
   Run after 04 (October and September are billed). */
USE TeleBillDB;
GO
 
-- Q1. Customers with no subscription (LEFT JOIN)
SELECT c.CustomerId, c.FullName
FROM dbo.Customers c
LEFT JOIN dbo.Subscriptions s ON s.CustomerId = c.CustomerId
WHERE s.SubscriptionId IS NULL;
 
-- Q2. Active subscriptions per plan
SELECT p.PlanName, COUNT(s.SubscriptionId) AS ActiveSubscriptions
FROM dbo.Plans p
LEFT JOIN dbo.Subscriptions s ON s.PlanId = p.PlanId AND s.Status = 'Active'
GROUP BY p.PlanName
ORDER BY ActiveSubscriptions DESC;
 
-- Q3. Total call minutes per plan in October
SELECT p.PlanName, SUM(u.Quantity) AS CallMinutes
FROM dbo.UsageRecords u
JOIN dbo.Subscriptions s ON s.SubscriptionId = u.SubscriptionId
JOIN dbo.Plans p ON p.PlanId = s.PlanId
WHERE u.UsageTypeId = 1
  AND u.UsageStart >= '2026-10-01' AND u.UsageStart < '2026-11-01'
GROUP BY p.PlanName
ORDER BY CallMinutes DESC;
 
-- Q4. Top 5 customers by data used in October
SELECT TOP (5) CustomerId, FullName, DataMB
FROM dbo.vw_CustomerMonthlyUsage
WHERE UsageMonth = '2026-10-01'
ORDER BY DataMB DESC;
 
-- Q5. Customers per city
SELECT City, COUNT(*) AS Customers
FROM dbo.Customers
GROUP BY City
ORDER BY Customers DESC;
 
-- Q6. Average call length per plan
SELECT p.PlanName, CAST(AVG(u.Quantity) AS DECIMAL(10,2)) AS AvgCallMinutes
FROM dbo.UsageRecords u
JOIN dbo.Subscriptions s ON s.SubscriptionId = u.SubscriptionId
JOIN dbo.Plans p ON p.PlanId = s.PlanId
WHERE u.UsageTypeId = 1
GROUP BY p.PlanName;
 
-- Q7. Total billed revenue per month
SELECT BillingMonth, SUM(TotalAmount) AS Revenue, COUNT(*) AS Invoices
FROM dbo.vw_InvoiceTotals
GROUP BY BillingMonth
ORDER BY BillingMonth;
 
-- Q8. October bill per customer (CTE)
WITH OctBills AS (
    SELECT CustomerId, SUM(TotalAmount) AS OctTotal
    FROM dbo.vw_InvoiceTotals
    WHERE BillingMonth = '2026-10-01'
    GROUP BY CustomerId
)
SELECT c.FullName, b.OctTotal
FROM OctBills b
JOIN dbo.Customers c ON c.CustomerId = b.CustomerId
ORDER BY b.OctTotal DESC;
 
-- Q9. Customers whose October bill is above average (CTE + subquery)
WITH OctBills AS (
    SELECT CustomerId, TotalAmount
    FROM dbo.vw_InvoiceTotals
    WHERE BillingMonth = '2026-10-01'
)
SELECT c.FullName, b.TotalAmount
FROM OctBills b
JOIN dbo.Customers c ON c.CustomerId = b.CustomerId
WHERE b.TotalAmount > (SELECT AVG(TotalAmount) FROM OctBills)
ORDER BY b.TotalAmount DESC;
 
-- Q10. Busiest day of the week for calls
SELECT DATENAME(WEEKDAY, UsageStart) AS DayName, SUM(Quantity) AS CallMinutes
FROM dbo.UsageRecords
WHERE UsageTypeId = 1
GROUP BY DATENAME(WEEKDAY, UsageStart)
ORDER BY CallMinutes DESC;
 
-- Q11. Usage of each type per month
SELECT DATEFROMPARTS(YEAR(u.UsageStart), MONTH(u.UsageStart), 1) AS UsageMonth,
       ut.TypeName, SUM(u.Quantity) AS Total, ut.Unit
FROM dbo.UsageRecords u
JOIN dbo.UsageTypes ut ON ut.UsageTypeId = u.UsageTypeId
GROUP BY DATEFROMPARTS(YEAR(u.UsageStart), MONTH(u.UsageStart), 1), ut.TypeName, ut.Unit
ORDER BY UsageMonth, ut.TypeName;
 
-- Q12. Subscriptions with no data usage in October (NOT EXISTS)
SELECT s.SubscriptionId, c.FullName
FROM dbo.Subscriptions s
JOIN dbo.Customers c ON c.CustomerId = s.CustomerId
WHERE NOT EXISTS (SELECT 1 FROM dbo.UsageRecords u
                  WHERE u.SubscriptionId = s.SubscriptionId
                    AND u.UsageTypeId = 3
                    AND u.UsageStart >= '2026-10-01' AND u.UsageStart < '2026-11-01');
 
-- Q13. Current rate card for every plan
SELECT p.PlanName, ut.TypeName, r.RatePerUnit, ut.Unit
FROM dbo.Rates r
JOIN dbo.Plans p ON p.PlanId = r.PlanId
JOIN dbo.UsageTypes ut ON ut.UsageTypeId = r.UsageTypeId
WHERE r.EffectiveTo IS NULL
ORDER BY p.PlanName, ut.TypeName;
 
-- Q14. Plans whose call rate changed during the year
SELECT p.PlanName, COUNT(*) AS CallRateVersions
FROM dbo.Rates r
JOIN dbo.Plans p ON p.PlanId = r.PlanId
WHERE r.UsageTypeId = 1
GROUP BY p.PlanName
HAVING COUNT(*) > 1;
 
-- Q15. Month-over-month change in call minutes per customer (LAG)
SELECT CustomerId, FullName, UsageMonth, CallMinutes,
       CallMinutes - LAG(CallMinutes) OVER (PARTITION BY CustomerId ORDER BY UsageMonth) AS ChangeVsLastMonth
FROM dbo.vw_CustomerMonthlyUsage
ORDER BY CustomerId, UsageMonth;
 
-- Q16. Rank customers by October bill (RANK)
SELECT c.FullName, b.TotalAmount,
       RANK() OVER (ORDER BY b.TotalAmount DESC) AS BillRank
FROM dbo.vw_InvoiceTotals b
JOIN dbo.Customers c ON c.CustomerId = b.CustomerId
WHERE b.BillingMonth = '2026-10-01';
 
-- Q17. Unbilled usage rows by month
SELECT DATEFROMPARTS(YEAR(UsageStart), MONTH(UsageStart), 1) AS UsageMonth,
       COUNT(*) AS UnbilledRows
FROM dbo.UsageRecords
WHERE InvoiceId IS NULL
GROUP BY DATEFROMPARTS(YEAR(UsageStart), MONTH(UsageStart), 1)
ORDER BY UsageMonth;
 
-- Q18. Largest single data session per customer (ROW_NUMBER + CTE)
WITH Ranked AS (
    SELECT s.CustomerId, u.UsageStart, u.Quantity,
           ROW_NUMBER() OVER (PARTITION BY s.CustomerId ORDER BY u.Quantity DESC, u.UsageStart) AS rn
    FROM dbo.UsageRecords u
    JOIN dbo.Subscriptions s ON s.SubscriptionId = u.SubscriptionId
    WHERE u.UsageTypeId = 3
)
SELECT c.FullName, r.UsageStart, r.Quantity AS DataMB
FROM Ranked r
JOIN dbo.Customers c ON c.CustomerId = r.CustomerId
WHERE r.rn = 1
ORDER BY r.Quantity DESC;
 
-- Q19. Postpaid customers with more than 2,000 call minutes in October (HAVING)
SELECT c.FullName, p.PlanName, SUM(u.Quantity) AS CallMinutes
FROM dbo.UsageRecords u
JOIN dbo.Subscriptions s ON s.SubscriptionId = u.SubscriptionId
JOIN dbo.Plans p ON p.PlanId = s.PlanId
JOIN dbo.Customers c ON c.CustomerId = s.CustomerId
WHERE p.PlanType = 'Postpaid'
  AND u.UsageTypeId = 1
  AND u.UsageStart >= '2026-10-01' AND u.UsageStart < '2026-11-01'
GROUP BY c.FullName, p.PlanName
HAVING SUM(u.Quantity) > 2000
ORDER BY CallMinutes DESC;
 
-- Q20. Share of October usage revenue by type (%)
WITH ByType AS (
    SELECT ut.TypeName, SUM(l.Amount) AS Amount
    FROM dbo.InvoiceLines l
    JOIN dbo.Invoices i ON i.InvoiceId = l.InvoiceId
    JOIN dbo.UsageTypes ut ON ut.UsageTypeId = l.UsageTypeId
    WHERE i.BillingMonth = '2026-10-01'
    GROUP BY ut.TypeName
)
SELECT TypeName, Amount,
       CAST(100.0 * Amount / SUM(Amount) OVER () AS DECIMAL(5,2)) AS PercentOfTotal
FROM ByType
ORDER BY Amount DESC;
GO
 