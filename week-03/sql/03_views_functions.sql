/*03_views_function.sql- task 5*/

USE TeleBillDB;
GO

CREATE OR ALTER FUNCTION dbo.fn_GetApplicableRate  --Function
(
@PlanID INT,
@UsageTypeId TINYINT,
@OnDate DATE
)

RETURNS TABLE
AS
RETURN
(
SELECT TOP (1) r.RateId, r.RatePerUnit
FROM dbo.Rates r
WHERE r.PlanId = @PlanId
AND r.UsageTypeId= @UsageTypeId
AND r.EffectiveFrom <= @OnDate --started on or before the date
AND (r.EffectiveTo IS NULL OR @OnDate < r.EffectiveTo) --and not ended yet
ORDER BY r.EffectiveFrom DESC --newest rate wins
);
GO

/* View: monthly usage per customer */
CREATE OR ALTER VIEW dbo.vw_CustomerMonthlyUsage
AS
SELECT c.CustomerId,
       c.FullName,
       DATEFROMPARTS(YEAR(u.UsageStart),MONTH(u.UsageStart), 1) AS UsageMonth,
       SUM(CASE WHEN u.UsageTypeId = 1 THEN u.Quantity ELSE 0 END) AS CallMinutes,
       SUM(CASE WHEN u.UsageTypeId = 2 THEN u.Quantity ELSE 0 END) AS SmsCount,
       SUM(CASE WHEN u.UsageTypeId = 3 THEN u.Quantity ELSE 0 END) AS DataMB
FROM dbo.Customers c
JOIN dbo.Subscriptions s ON s.CustomerId = c.CustomerId
JOIN dbo.UsageRecords u  ON u.SubscriptionId = s.SubscriptionId
GROUP BY c.CustomerId, c.FullName,
         DATEFROMPARTS(YEAR(u.UsageStart), MONTH(u.UsageStart), 1);
GO
 
/* View: invoice totals (used later by the business questions) */
CREATE OR ALTER VIEW dbo.vw_InvoiceTotals
AS
SELECT i.InvoiceId,
       i.SubscriptionId,
       s.CustomerId,
       i.BillingMonth,
       i.PlanFee,
       ISNULL(SUM(l.Amount), 0)  AS UsageAmount,
       i.PlanFee + ISNULL(SUM(l.Amount), 0) AS TotalAmount
FROM dbo.Invoices i
JOIN dbo.Subscriptions s ON s.SubscriptionId = i.SubscriptionId
LEFT JOIN dbo.InvoiceLines l ON l.InvoiceId = i.InvoiceId -- invoice may have no lines
GROUP BY i.InvoiceId, i.SubscriptionId, s.CustomerId, i.BillingMonth, i.PlanFee;
GO
 
/* Tests */
SELECT * FROM dbo.fn_GetApplicableRate(2, 1, '2026-10-10'); -- before the price rise: 1.0000
SELECT * FROM dbo.fn_GetApplicableRate(2, 1, '2026-10-20'); -- after the price rise:  1.2000
SELECT TOP (10) * FROM dbo.vw_CustomerMonthlyUsage ORDER BY CustomerId, UsageMonth;
GO