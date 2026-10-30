/* audit_triggers: 
Records the old and new value whenever a plan's rate changes.*/


USE TeleBillDB;
GO

CREATE OR ALTER TRIGGER dbo. trg_Rates_Audit
ON dbo.Rates
AFTER UPDATE --runs after every UPDATE on RATES
AS 
BEGIN
SET NOCOUNT ON; --stops sql server from printing a "(n rows affected)" msg after every statement
    IF NOT UPDATE(RatePerUnit) RETURN; --rate col not touched- nothing to log

    INSERT INTO dbo.RateAudit (RateId, PlanId, UsageTypeId, OldRate, NewRate)
    SELECT d.RateId, d.PlanId, d.UsageTypeId,
           d.RatePerUnit, -- deleted  = value before the update
           i.RatePerUnit -- inserted = value after the update
    FROM deleted d
    JOIN inserted i ON i.RateId = d.RateId -- works for 1 row or many rows
    WHERE d.RatePerUnit <> i.RatePerUnit; -- only real changes
END;
GO
 
/* TESTS */
 
-- 1. Change one rate: Smart Prepaid SMS 0.40 -> 0.45
UPDATE dbo.Rates SET RatePerUnit = 0.4500
WHERE PlanId = 3 AND UsageTypeId = 2 AND EffectiveTo IS NULL;
 
-- 2. Change many rates in one statement: all current data rates +10%
UPDATE dbo.Rates SET RatePerUnit = RatePerUnit * 1.10
WHERE UsageTypeId = 3 AND EffectiveTo IS NULL;
 
-- 3. An update that changes nothing: should NOT be logged
UPDATE dbo.Rates SET RatePerUnit = RatePerUnit WHERE PlanId = 1;
 
-- See the audit log
SELECT a.AuditId, p.PlanName, ut.TypeName, a.OldRate, a.NewRate, a.ChangedAt, a.ChangedBy
FROM dbo.RateAudit a
JOIN dbo.Plans p  ON p.PlanId = a.PlanId
JOIN dbo.UsageTypes ut ON ut.UsageTypeId = a.UsageTypeId
ORDER BY a.AuditId;
GO