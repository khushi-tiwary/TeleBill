# Performance Note – Indexing UsageRecords
 
## The slow query
One subscription's usage for one month, the query the app runs most:
 
SELECT u.UsageTypeId, COUNT(*) AS Sessions, SUM(u.Quantity) AS Total
FROM dbo.UsageRecords u
WHERE u.SubscriptionId = 10
  AND u.UsageStart >= '2026-10-01' AND u.UsageStart < '2026-11-01'
GROUP BY u.UsageTypeId;
 
## The index
CREATE NONCLUSTERED INDEX IX_Usage_Sub_Start
ON dbo.UsageRecords (SubscriptionId, UsageStart)
INCLUDE (UsageTypeId, Quantity, InvoiceId);
 
- Key columns match the WHERE clause: SubscriptionId (equality) first, UsageStart (range) second.
- INCLUDE holds the other columns the query reads, so there is no key lookup.
- SQL Server's own plan flagged a Missing Index (impact 95.3%) on these columns before it was created.
 
## Results (120,000 usage rows)
 
| | Before (no index) | After (IX_Usage_Sub_Start) |
|---|---|---|
| Plan operator | Clustered Index Scan | Index Seek |
| Logical reads | 614 | 8 |
| CPU time | 16 ms | 0 ms |
| Elapsed time | 30 ms | 25 ms |
 
Logical reads dropped from 614 to 8 (about 77x fewer pages read).
 
Plans: plan-before.png, plan-after.png
 
## Why it's faster
Without an index SQL Server scans all 120,000 rows to find one subscription's month. The new index is sorted by subscription, then date, so it seeks straight to that range and reads only those rows. Because the index covers every column the query needs, it never goes back to the table.
 
Elapsed time changes little at this size because the table fits in memory. Logical reads show the real difference, and that gap grows as the table grows.
 
## Trade-off
Every insert into UsageRecords must also update this index, so writes are slightly slower and the index takes extra space. Worth it because usage is read far more often than individual rows are written.
 