-- ============================================================================
-- CA OFFICE WORKFORCE PRODUCTIVITY AUTOMATION SYSTEM
-- MIGRATION 13: COMPLIANCE AUTOMATION AND AUDIT TRAIL TABLES
-- Idempotent DDL Migration Script for Phase 12.6 Compliance Automation & Audit System
-- ============================================================================

-- 1. dbo.tbl_ComplianceRules
IF OBJECT_ID(N'dbo.tbl_ComplianceRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ComplianceRules (
        ComplianceRuleId INT IDENTITY(1,1) NOT NULL,
        ComplianceName NVARCHAR(128) NOT NULL,
        Category NVARCHAR(50) NOT NULL, -- GST, TDS, Income Tax, Tax Audit, Corporate, Other
        Frequency NVARCHAR(50) NOT NULL, -- Monthly, Quarterly, Annual, Custom
        DueDateRuleDay INT NOT NULL DEFAULT 11,
        DueDateRuleMonth INT NULL,
        Priority NVARCHAR(50) NOT NULL DEFAULT 'High', -- Normal, High, Critical
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_ComplianceRules_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_ComplianceRules_CreatedBy DEFAULT (1),
        ModifiedOn DATETIME2 NULL,
        ModifiedBy INT NULL,
        CONSTRAINT PK_tbl_ComplianceRules PRIMARY KEY CLUSTERED (ComplianceRuleId ASC)
    );
END;
GO

-- 2. dbo.tbl_ComplianceAlertSchedules
IF OBJECT_ID(N'dbo.tbl_ComplianceAlertSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ComplianceAlertSchedules (
        AlertScheduleId INT IDENTITY(1,1) NOT NULL,
        ComplianceRuleId INT NOT NULL,
        DaysBeforeDeadline INT NOT NULL, -- Days before (positive) or after (negative) deadline
        AlertType NVARCHAR(50) NOT NULL DEFAULT 'Reminder', -- Reminder, Urgent, Overdue
        IsEnabled BIT NOT NULL DEFAULT 1,
        CONSTRAINT PK_tbl_ComplianceAlertSchedules PRIMARY KEY CLUSTERED (AlertScheduleId ASC),
        CONSTRAINT FK_tbl_ComplianceAlertSchedules_Rules FOREIGN KEY (ComplianceRuleId) REFERENCES dbo.tbl_ComplianceRules (ComplianceRuleId) ON DELETE CASCADE
    );
END;
GO

-- 3. dbo.tbl_ComplianceAutomationHistory
IF OBJECT_ID(N'dbo.tbl_ComplianceAutomationHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ComplianceAutomationHistory (
        HistoryId INT IDENTITY(1,1) NOT NULL,
        ComplianceRuleId INT NOT NULL,
        CompliancePeriod NVARCHAR(50) NOT NULL, -- e.g., "August 2026", "Q2 2026-27"
        DeadlineDate DATETIME2 NOT NULL,
        TriggerDate DATETIME2 NOT NULL,
        TaskId INT NULL, -- FK to tbl_Tasks
        AlertGenerated BIT NOT NULL DEFAULT 1,
        TaskGenerated BIT NOT NULL DEFAULT 1,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Triggered', -- Scheduled, Triggered, Completed, Failed
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_ComplianceHistory_CreatedOn DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_tbl_ComplianceAutomationHistory PRIMARY KEY CLUSTERED (HistoryId ASC),
        CONSTRAINT FK_tbl_ComplianceHistory_Rules FOREIGN KEY (ComplianceRuleId) REFERENCES dbo.tbl_ComplianceRules (ComplianceRuleId)
    );

    CREATE INDEX IX_tbl_ComplianceHistory_Lookup ON dbo.tbl_ComplianceAutomationHistory (ComplianceRuleId, CompliancePeriod);
END;
GO

-- 4. Idempotent Default Compliance Rules Seeding
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ComplianceRules WHERE ComplianceName = N'GSTR-1 Monthly Return')
BEGIN
    INSERT INTO dbo.tbl_ComplianceRules (ComplianceName, Category, Frequency, DueDateRuleDay, Priority, IsActive)
    VALUES (N'GSTR-1 Monthly Return', N'GST', N'Monthly', 11, N'High', 1);

    DECLARE @RuleGstr1 INT = SCOPE_IDENTITY();
    INSERT INTO dbo.tbl_ComplianceAlertSchedules (ComplianceRuleId, DaysBeforeDeadline, AlertType, IsEnabled)
    VALUES (@RuleGstr1, 10, N'Reminder', 1), (@RuleGstr1, 3, N'Urgent', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ComplianceRules WHERE ComplianceName = N'GSTR-3B Summary Return')
BEGIN
    INSERT INTO dbo.tbl_ComplianceRules (ComplianceName, Category, Frequency, DueDateRuleDay, Priority, IsActive)
    VALUES (N'GSTR-3B Summary Return', N'GST', N'Monthly', 20, N'High', 1);

    DECLARE @RuleGstr3b INT = SCOPE_IDENTITY();
    INSERT INTO dbo.tbl_ComplianceAlertSchedules (ComplianceRuleId, DaysBeforeDeadline, AlertType, IsEnabled)
    VALUES (@RuleGstr3b, 7, N'Reminder', 1), (@RuleGstr3b, 2, N'Urgent', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ComplianceRules WHERE ComplianceName = N'Quarterly TDS Return Filing')
BEGIN
    INSERT INTO dbo.tbl_ComplianceRules (ComplianceName, Category, Frequency, DueDateRuleDay, Priority, IsActive)
    VALUES (N'Quarterly TDS Return Filing', N'TDS', N'Quarterly', 31, N'High', 1);

    DECLARE @RuleTds INT = SCOPE_IDENTITY();
    INSERT INTO dbo.tbl_ComplianceAlertSchedules (ComplianceRuleId, DaysBeforeDeadline, AlertType, IsEnabled)
    VALUES (@RuleTds, 15, N'Reminder', 1), (@RuleTds, 5, N'Urgent', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ComplianceRules WHERE ComplianceName = N'Tax Audit Filing (Sec 44AB)')
BEGIN
    INSERT INTO dbo.tbl_ComplianceRules (ComplianceName, Category, Frequency, DueDateRuleDay, DueDateRuleMonth, Priority, IsActive)
    VALUES (N'Tax Audit Filing (Sec 44AB)', N'Tax Audit', N'Annual', 30, 9, N'Critical', 1);

    DECLARE @RuleAudit INT = SCOPE_IDENTITY();
    INSERT INTO dbo.tbl_ComplianceAlertSchedules (ComplianceRuleId, DaysBeforeDeadline, AlertType, IsEnabled)
    VALUES (@RuleAudit, 30, N'Reminder', 1), (@RuleAudit, 10, N'Urgent', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ComplianceRules WHERE ComplianceName = N'Income Tax Return (ITR-3/ITR-6)')
BEGIN
    INSERT INTO dbo.tbl_ComplianceRules (ComplianceName, Category, Frequency, DueDateRuleDay, DueDateRuleMonth, Priority, IsActive)
    VALUES (N'Income Tax Return (ITR-3/ITR-6)', N'Income Tax', N'Annual', 31, 10, N'Critical', 1);

    DECLARE @RuleItr INT = SCOPE_IDENTITY();
    INSERT INTO dbo.tbl_ComplianceAlertSchedules (ComplianceRuleId, DaysBeforeDeadline, AlertType, IsEnabled)
    VALUES (@RuleItr, 30, N'Reminder', 1), (@RuleItr, 10, N'Urgent', 1);
END;
GO
