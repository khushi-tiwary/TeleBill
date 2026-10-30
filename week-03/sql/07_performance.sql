/* 07_performance.sql - Task 7
   Slow query -> add the right index -> compare plans and timings.. */

USE TeleBillDB;
GO
DROP INDEX IF EXISTS IX_Usage_Sub_Start ON dbo.UsageRecords; -- start with no index
GO
 
SET STATISTICS IO ON; -- shows "logical reads" in Messages
SET STATISTICS TIME ON; -- shows CPU and elapsed time in Messages
GO
 
-- BEFORE: no index
SELECT u.UsageTypeId, COUNT(*) AS Sessions, SUM(u.Quantity) AS Total
FROM dbo.UsageRecords u
WHERE u.SubscriptionId = 10
  AND u.UsageStart >= '2026-10-01' AND u.UsageStart < '2026-11-01'
GROUP BY u.UsageTypeId;
GO
 
-- The right index: WHERE columns as keys, other columns in INCLUDE
CREATE NONCLUSTERED INDEX IX_Usage_Sub_Start
ON dbo.UsageRecords (SubscriptionId, UsageStart)
INCLUDE (UsageTypeId, Quantity, InvoiceId);
GO
 
-- AFTER: same query
SELECT u.UsageTypeId, COUNT(*) AS Sessions, SUM(u.Quantity) AS Total
FROM dbo.UsageRecords u
WHERE u.SubscriptionId = 10
  AND u.UsageStart >= '2026-10-01' AND u.UsageStart < '2026-11-01'
GROUP BY u.UsageTypeId;
GO
 
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO
 