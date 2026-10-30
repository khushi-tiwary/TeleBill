USE master;
GO
IF DB_ID('TeleBillDB') IS NOT NULL                        -- drop old copy if it exists
BEGIN
    ALTER DATABASE TeleBillDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TeleBillDB;
END
GO
CREATE DATABASE TeleBillDB;
GO
USE TeleBillDB;
GO
 
/* ---------- Customers ---------- */
CREATE TABLE dbo.Customers (
    CustomerId INT IDENTITY(1000,1) NOT NULL,
    FullName   NVARCHAR(100) NOT NULL,
    Email      VARCHAR(255)  NULL,                        -- optional
    Phone      VARCHAR(15)   NOT NULL,                    -- text: keeps leading 0 / +
    City       NVARCHAR(60)  NULL,
    CreatedAt  DATETIME2(0)  NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Customers       PRIMARY KEY (CustomerId),
    CONSTRAINT UQ_Customers_Phone UNIQUE (Phone),
    CONSTRAINT CK_Customers_Phone CHECK (Phone NOT LIKE '%[^0-9+]%' AND LEN(Phone) >= 10)
);
 
/* ---------- Plans ---------- */
CREATE TABLE dbo.Plans (
    PlanId     INT IDENTITY(1,1) NOT NULL,
    PlanCode   VARCHAR(20)   NOT NULL,
    PlanName   NVARCHAR(100) NOT NULL,
    PlanType   VARCHAR(10)   NOT NULL,
    MonthlyFee DECIMAL(10,2) NOT NULL,                    -- money = DECIMAL
    IsActive   BIT           NOT NULL CONSTRAINT DF_Plans_IsActive DEFAULT 1,
    CONSTRAINT PK_Plans          PRIMARY KEY (PlanId),
    CONSTRAINT UQ_Plans_PlanCode UNIQUE (PlanCode),
    CONSTRAINT CK_Plans_PlanType CHECK (PlanType IN ('Prepaid','Postpaid')),
    CONSTRAINT CK_Plans_Fee      CHECK (MonthlyFee >= 0)
);
 
/* ---------- UsageTypes (lookup: Call / SMS / Data) ---------- */
CREATE TABLE dbo.UsageTypes (
    UsageTypeId TINYINT     NOT NULL,                     -- 1 byte, small fixed list
    TypeName    VARCHAR(10) NOT NULL,                     -- Call, SMS, Data
    Unit        VARCHAR(10) NOT NULL,                     -- minute, message, MB
    CONSTRAINT PK_UsageTypes      PRIMARY KEY (UsageTypeId),
    CONSTRAINT UQ_UsageTypes_Name UNIQUE (TypeName)
);
 
/* ---------- Rates (price per unit, valid for a date range) ---------- */
CREATE TABLE dbo.Rates (
    RateId        INT IDENTITY(1,1) NOT NULL,
    PlanId        INT           NOT NULL,
    UsageTypeId   TINYINT       NOT NULL,
    RatePerUnit   DECIMAL(10,4) NOT NULL,                 -- 4 decimals: data is priced per MB
    EffectiveFrom DATE          NOT NULL,                 -- inclusive
    EffectiveTo   DATE          NULL,                     -- exclusive; NULL = current rate
    CONSTRAINT PK_Rates              PRIMARY KEY (RateId),
    CONSTRAINT FK_Rates_Plans        FOREIGN KEY (PlanId)      REFERENCES dbo.Plans(PlanId),
    CONSTRAINT FK_Rates_UsageTypes   FOREIGN KEY (UsageTypeId) REFERENCES dbo.UsageTypes(UsageTypeId),
    CONSTRAINT UQ_Rates_PlanTypeFrom UNIQUE (PlanId, UsageTypeId, EffectiveFrom),
    CONSTRAINT CK_Rates_Rate         CHECK (RatePerUnit >= 0),
    CONSTRAINT CK_Rates_Dates        CHECK (EffectiveTo IS NULL OR EffectiveTo > EffectiveFrom)
);
 
/* ---------- Subscriptions (customer <-> plan) ---------- */
CREATE TABLE dbo.Subscriptions (
    SubscriptionId INT IDENTITY(1,1) NOT NULL,
    CustomerId     INT         NOT NULL,
    PlanId         INT         NOT NULL,
    StartDate      DATE        NOT NULL CONSTRAINT DF_Subs_StartDate DEFAULT CAST(GETDATE() AS DATE),
    EndDate        DATE        NULL,                      -- NULL = still running
    Status         VARCHAR(10) NOT NULL CONSTRAINT DF_Subs_Status DEFAULT 'Active',
    CONSTRAINT PK_Subscriptions  PRIMARY KEY (SubscriptionId),
    CONSTRAINT FK_Subs_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT FK_Subs_Plans     FOREIGN KEY (PlanId)     REFERENCES dbo.Plans(PlanId),
    CONSTRAINT CK_Subs_Status    CHECK (Status IN ('Active','Suspended','Cancelled')),
    CONSTRAINT CK_Subs_Dates     CHECK (EndDate IS NULL OR EndDate >= StartDate)
);
 
/* ---------- Invoices (one per subscription per month) ---------- */
CREATE TABLE dbo.Invoices (
    InvoiceId      INT IDENTITY(1,1) NOT NULL,
    SubscriptionId INT           NOT NULL,
    BillingMonth   DATE          NOT NULL,                -- always the 1st, e.g. 2026-10-01
    PlanFee        DECIMAL(10,2) NOT NULL,                -- fee at billing time (snapshot)
    CreatedAt      DATETIME2(0)  NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Invoices          PRIMARY KEY (InvoiceId),
    CONSTRAINT FK_Invoices_Subs     FOREIGN KEY (SubscriptionId) REFERENCES dbo.Subscriptions(SubscriptionId),
    CONSTRAINT UQ_Invoices_SubMonth UNIQUE (SubscriptionId, BillingMonth),   -- never bill a month twice
    CONSTRAINT CK_Invoices_Month    CHECK (DAY(BillingMonth) = 1),
    CONSTRAINT CK_Invoices_Fee      CHECK (PlanFee >= 0)
);
 
/* ---------- InvoiceLines (usage charges on a bill) ---------- */
CREATE TABLE dbo.InvoiceLines (
    InvoiceLineId INT IDENTITY(1,1) NOT NULL,
    InvoiceId     INT           NOT NULL,
    UsageTypeId   TINYINT       NOT NULL,
    Quantity      DECIMAL(14,2) NOT NULL,
    RatePerUnit   DECIMAL(10,4) NOT NULL,                 -- rate used at billing time (snapshot)
    Amount        DECIMAL(14,2) NOT NULL,
    CONSTRAINT PK_InvoiceLines        PRIMARY KEY (InvoiceLineId),
    CONSTRAINT FK_InvLines_Invoices   FOREIGN KEY (InvoiceId)   REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT FK_InvLines_UsageTypes FOREIGN KEY (UsageTypeId) REFERENCES dbo.UsageTypes(UsageTypeId),
    CONSTRAINT CK_InvLines_Qty        CHECK (Quantity >= 0),
    CONSTRAINT CK_InvLines_Amount     CHECK (Amount >= 0)
);
 
/* ---------- UsageRecords (every call / SMS / data session) ---------- */
CREATE TABLE dbo.UsageRecords (
    UsageId        BIGINT IDENTITY(1,1) NOT NULL,         -- BIGINT: fastest-growing table
    SubscriptionId INT           NOT NULL,
    UsageTypeId    TINYINT       NOT NULL,
    UsageStart     DATETIME2(0)  NOT NULL,                -- when it happened
    Quantity       DECIMAL(12,2) NOT NULL,                -- minutes / messages / MB
    InvoiceId      INT           NULL,                    -- NULL = not billed yet
    CONSTRAINT PK_UsageRecords     PRIMARY KEY (UsageId),
    CONSTRAINT FK_Usage_Subs       FOREIGN KEY (SubscriptionId) REFERENCES dbo.Subscriptions(SubscriptionId),
    CONSTRAINT FK_Usage_UsageTypes FOREIGN KEY (UsageTypeId)    REFERENCES dbo.UsageTypes(UsageTypeId),
    CONSTRAINT FK_Usage_Invoices   FOREIGN KEY (InvoiceId)      REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT CK_Usage_Qty        CHECK (Quantity > 0)
);
 
/* ---------- RateAudit (filled by the trigger in 05) ---------- */
CREATE TABLE dbo.RateAudit (                              -- no FKs: the log must outlive the rate
    AuditId     INT IDENTITY(1,1) NOT NULL,
    RateId      INT           NOT NULL,
    PlanId      INT           NOT NULL,
    UsageTypeId TINYINT       NOT NULL,
    OldRate     DECIMAL(10,4) NOT NULL,
    NewRate     DECIMAL(10,4) NOT NULL,
    ChangedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_RateAudit_ChangedAt DEFAULT SYSUTCDATETIME(),
    ChangedBy   NVARCHAR(128) NOT NULL CONSTRAINT DF_RateAudit_ChangedBy DEFAULT SUSER_SNAME(),
    CONSTRAINT PK_RateAudit PRIMARY KEY (AuditId)
);
GO
 
PRINT '01_schema.sql completed: 9 tables created.';