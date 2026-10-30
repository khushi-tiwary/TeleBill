/* 04_rate_usage.sql - Task 6
   usp_RateUsage(@BillingMonth): rates all unbilled usage for a month,
   writes Invoices + InvoiceLines and marks usage as billed.
   One transaction, TRY/CATCH, rollback. Safe to re-run for the same month. */
USE TeleBillDB;
GO
 
CREATE OR ALTER PROCEDURE dbo.usp_RateUsage
    @BillingMonth DATE,
    @SimulateFailure BIT = 0  -- testing only: fail halfway on purpose
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;  -- any error cancels the whole transaction
 
    SET @BillingMonth = DATEFROMPARTS(YEAR(@BillingMonth), MONTH(@BillingMonth), 1);  -- force 1st of month
    DECLARE @NextMonth DATE = DATEADD(MONTH, 1, @BillingMonth);
 
    BEGIN TRY
        BEGIN TRANSACTION;
 
        /* 1. Rate every unbilled usage row in the month */
        CREATE TABLE #Rated (
            UsageId BIGINT PRIMARY KEY,
            SubscriptionId INT NOT NULL,
            UsageTypeId TINYINT NOT NULL,
            Quantity DECIMAL(12,2) NOT NULL,
            RatePerUnit DECIMAL(10,4) NOT NULL
        );
 
        INSERT INTO #Rated (UsageId, SubscriptionId, UsageTypeId, Quantity, RatePerUnit)
        SELECT u.UsageId, u.SubscriptionId, u.UsageTypeId, u.Quantity, r.RatePerUnit
        FROM dbo.UsageRecords u
        JOIN dbo.Subscriptions s ON s.SubscriptionId = u.SubscriptionId
        CROSS APPLY dbo.fn_GetApplicableRate(s.PlanId, u.UsageTypeId, CAST(u.UsageStart AS DATE)) r
        WHERE u.InvoiceId IS NULL  -- only unbilled -> safe to re-run
          AND u.UsageStart >= @BillingMonth
          AND u.UsageStart <  @NextMonth;
 
        /* Every unbilled row must have found a rate, otherwise stop */
        DECLARE @Unbilled INT =
            (SELECT COUNT(*) FROM dbo.UsageRecords
             WHERE InvoiceId IS NULL AND UsageStart >= @BillingMonth AND UsageStart < @NextMonth);
 
        IF @Unbilled <> (SELECT COUNT(*) FROM #Rated)
            THROW 50001, 'Some usage has no applicable rate. Nothing was billed.', 1;
 
        /* 2. Create invoices for subscriptions active in the month that don't have one yet */
        INSERT INTO dbo.Invoices (SubscriptionId, BillingMonth, PlanFee)
        SELECT s.SubscriptionId, @BillingMonth, p.MonthlyFee
        FROM dbo.Subscriptions s
        JOIN dbo.Plans p ON p.PlanId = s.PlanId
        WHERE (   (s.StartDate < @NextMonth  -- active during the month
                   AND (s.EndDate IS NULL OR s.EndDate >= @BillingMonth))
               OR s.SubscriptionId IN (SELECT SubscriptionId FROM #Rated)) -- or has usage to bill
          AND NOT EXISTS (SELECT 1 FROM dbo.Invoices i  -- and not billed yet
                          WHERE i.SubscriptionId = s.SubscriptionId
                            AND i.BillingMonth = @BillingMonth);
 
        IF @SimulateFailure = 1
            THROW 50002, 'Simulated failure after creating invoices.', 1;
 
        /* 3. Invoice lines: quantity x rate, per usage type and rate */
        INSERT INTO dbo.InvoiceLines (InvoiceId, UsageTypeId, Quantity, RatePerUnit, Amount)
        SELECT i.InvoiceId,
               r.UsageTypeId,
               SUM(r.Quantity),
               r.RatePerUnit,
               ROUND(SUM(r.Quantity) * r.RatePerUnit, 2)
        FROM #Rated r
        JOIN dbo.Invoices i ON i.SubscriptionId = r.SubscriptionId
                           AND i.BillingMonth   = @BillingMonth
        GROUP BY i.InvoiceId, r.UsageTypeId, r.RatePerUnit; -- rate change mid-month = 2 lines
 
        /* 4. Mark the usage as billed */
        UPDATE u
        SET    u.InvoiceId = i.InvoiceId
        FROM dbo.UsageRecords u
        JOIN #Rated r  ON r.UsageId = u.UsageId
        JOIN dbo.Invoices i ON i.SubscriptionId = r.SubscriptionId
                           AND i.BillingMonth = @BillingMonth;
 
        COMMIT TRANSACTION;  -- everything worked: save it all
 
        /* Summary */
        SELECT @BillingMonth  AS BillingMonth,
               (SELECT COUNT(*) FROM #Rated)   AS UsageRowsBilled,
               (SELECT COUNT(*) FROM dbo.Invoices WHERE BillingMonth = @BillingMonth) AS InvoicesForMonth;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION; -- undo everything from this run
        THROW;  -- pass the error to the caller
    END CATCH
END;
GO
 
/* TESTS  */
 
/* Test 1: bill October */
EXEC dbo.usp_RateUsage @BillingMonth = '2026-10-01';
GO
 
/* Test 2: run again - nothing new is billed (safe to re-run) */
EXEC dbo.usp_RateUsage @BillingMonth = '2026-10-01';
GO
 
/* Test 3: fail halfway through September - everything is rolled back */
BEGIN TRY
    EXEC dbo.usp_RateUsage @BillingMonth = '2026-09-01', @SimulateFailure = 1;
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH;
SELECT COUNT(*) AS SeptemberInvoices FROM dbo.Invoices WHERE BillingMonth = '2026-09-01';
GO
 
/* Test 4: bill September properly */
EXEC dbo.usp_RateUsage @BillingMonth = '2026-09-01';
GO
 
/* Test 5: Basic Prepaid in October has two call lines (price rise on 15 Oct) */
SELECT TOP (10) i.InvoiceId, i.SubscriptionId, ut.TypeName, l.Quantity, l.RatePerUnit, l.Amount
FROM dbo.InvoiceLines l
JOIN dbo.Invoices i ON i.InvoiceId = l.InvoiceId
JOIN dbo.UsageTypes ut ON ut.UsageTypeId = l.UsageTypeId
JOIN dbo.Subscriptions s ON s.SubscriptionId = i.SubscriptionId
WHERE s.PlanId = 2 AND i.BillingMonth = '2026-10-01'
ORDER BY i.InvoiceId, ut.TypeName, l.RatePerUnit;
GO
 