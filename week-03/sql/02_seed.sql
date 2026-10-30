USE TeleBillDB;
GO
SET NOCOUNT ON;
 
/* ---------- 1. Usage types (fixed list) ---------- */
INSERT INTO dbo.UsageTypes (UsageTypeId, TypeName, Unit)
VALUES (1, 'Call', 'minute'),
       (2, 'SMS',  'message'),
       (3, 'Data', 'MB');
 
/* ---------- 2. Plans ---------- */
INSERT INTO dbo.Plans (PlanCode, PlanName, PlanType, MonthlyFee)
VALUES ('PRE-99',  N'Starter',         'Prepaid',  99),     -- PlanId 1
       ('PRE-199', N'Basic Prepaid',   'Prepaid',  199),    -- 2
       ('PRE-399', N'Smart Prepaid',   'Prepaid',  399),    -- 3
       ('POS-599', N'Family Postpaid', 'Postpaid', 599),    -- 4
       ('POS-999', N'Unlimited Pro',   'Postpaid', 999);    -- 5
 
/* ---------- 3. Rates (Call per minute, SMS per message, Data per MB) ---------- */
INSERT INTO dbo.Rates (PlanId, UsageTypeId, RatePerUnit, EffectiveFrom, EffectiveTo)
VALUES (1, 1, 1.2000, '2026-01-01', NULL), (1, 2, 0.6000, '2026-01-01', NULL), (1, 3, 0.0800, '2026-01-01', NULL),
       (2, 1, 1.0000, '2026-01-01', '2026-10-15'),       -- Basic call rate until 14 Oct...
       (2, 1, 1.2000, '2026-10-15', NULL),               -- ...then a price rise from 15 Oct
       (2, 2, 0.5000, '2026-01-01', NULL), (2, 3, 0.0500, '2026-01-01', NULL),
       (3, 1, 0.8000, '2026-01-01', NULL), (3, 2, 0.4000, '2026-01-01', NULL), (3, 3, 0.0400, '2026-01-01', NULL),
       (4, 1, 0.6000, '2026-01-01', NULL), (4, 2, 0.3000, '2026-01-01', NULL), (4, 3, 0.0200, '2026-01-01', NULL),
       (5, 1, 0.0000, '2026-01-01', NULL), (5, 2, 0.0000, '2026-01-01', NULL), (5, 3, 0.0100, '2026-01-01', NULL);
 
/* ---------- 4. 50 customers from a numbers table ---------- */
WITH Numbers AS (
    SELECT TOP (50) CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS INT) AS n    -- 1..50
    FROM sys.all_objects
)
INSERT INTO dbo.Customers (FullName, Email, Phone, City)
SELECT nm.FirstName + N' ' + nm.LastName,
       CASE WHEN n % 7 = 0 THEN NULL                                     -- every 7th has no email
            ELSE LOWER(nm.FirstName) + '.' + LOWER(nm.LastName) + '@example.com' END,
       '9' + RIGHT('000000000' + CAST(n AS VARCHAR(9)), 9),              -- unique 10-digit phone
       CHOOSE(n % 6 + 1, N'Bengaluru', N'Mumbai', N'Delhi', N'Chennai', N'Hyderabad', N'Kolkata')
FROM Numbers
CROSS APPLY (SELECT CHOOSE(n % 10 + 1, N'Aarav', N'Priya', N'Rohan', N'Meera', N'Arjun',
                                       N'Sneha', N'Vikram', N'Ananya', N'Kabir', N'Diya') AS FirstName,
                    CHOOSE((n / 10) % 5 + 1, N'Sharma', N'Nair', N'Das', N'Iyer', N'Reddy') AS LastName) nm
ORDER BY n;                                                              -- CustomerId 1000..1049
 
/* ---------- 5. 48 subscriptions (customers 1048, 1049 have none) ---------- */
WITH Numbers AS (
    SELECT TOP (48) CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS INT) AS n     -- 1..48
    FROM sys.all_objects
)
INSERT INTO dbo.Subscriptions (CustomerId, PlanId, StartDate, Status)
SELECT 999 + n,                                                         -- customers 1000..1047
       n % 5 + 1,                                                       -- spread over 5 plans
       '2026-07-01',
       CASE WHEN n % 12 = 0 THEN 'Suspended' ELSE 'Active' END
FROM Numbers
ORDER BY n;                                                             -- SubscriptionId 1..48
 
/* ---------- 6. 120,000 usage rows (Aug-Oct 2026) from a numbers table ---------- */
WITH Numbers AS (
    SELECT TOP (120000) CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS INT) AS n   -- INT: DATEADD rejects BIGINT
    FROM sys.all_objects a
    CROSS JOIN sys.all_objects b                                        -- millions of combinations
)
INSERT INTO dbo.UsageRecords (SubscriptionId, UsageTypeId, UsageStart, Quantity)
SELECT n % 48 + 1,                                                      -- subscription 1..48
       t.UsageTypeId,
       DATEADD(MINUTE, (n * 37) % 132480, '2026-08-01'),                -- 132480 min = 92 days (Aug-Oct)
       CASE t.UsageTypeId
            WHEN 1 THEN n % 30 + 1                                      -- call: 1-30 minutes
            WHEN 2 THEN 1                                               -- SMS: 1 message
            ELSE n % 500 + 1                                            -- data: 1-500 MB
       END
FROM Numbers
CROSS APPLY (SELECT CAST((n / 48) % 3 + 1 AS TINYINT) AS UsageTypeId) t;  -- type 1..3
 
/* ---------- Check ---------- */
SELECT 'Customers' AS TableName, COUNT(*) AS Rows FROM dbo.Customers
UNION ALL SELECT 'Plans',         COUNT(*) FROM dbo.Plans
UNION ALL SELECT 'Rates',         COUNT(*) FROM dbo.Rates
UNION ALL SELECT 'Subscriptions', COUNT(*) FROM dbo.Subscriptions
UNION ALL SELECT 'UsageRecords',  COUNT(*) FROM dbo.UsageRecords;
-- Expect 50, 5, 16, 48, 120000
GO
 