-- ============================================================================
-- CA OFFICE WORKFORCE PRODUCTIVITY AUTOMATION SYSTEM
-- MIGRATION 12: DISASTER RECOVERY & BACKUP AUDIT TABLES
-- Idempotent DDL Migration Script for Phase 12 Database Protection Console
-- ============================================================================

IF OBJECT_ID(N'dbo.tbl_DatabaseBackupHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_DatabaseBackupHistory (
        BackupId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DatabaseName NVARCHAR(128) NOT NULL,
        BackupType NVARCHAR(50) NOT NULL,
        FilePath NVARCHAR(512) NOT NULL,
        FileName NVARCHAR(256) NOT NULL,
        FileSizeBytes BIGINT NOT NULL DEFAULT 0,
        Sha256Hash NVARCHAR(64) NULL,
        StartedOn DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CompletedOn DATETIME2 NULL,
        Status NVARCHAR(50) NOT NULL, -- Pending, Running, Succeeded, Failed, VerificationFailed
        VerificationStatus NVARCHAR(50) NULL, -- Verified, VerificationFailed, Skipped
        VerificationMessage NVARCHAR(MAX) NULL,
        SecondaryCopyStatus NVARCHAR(50) NULL, -- None, Pending, Succeeded, Failed
        SecondaryFilePath NVARCHAR(512) NULL,
        SqlServerVersion NVARCHAR(128) NULL,
        SqlServerEdition NVARCHAR(128) NULL,
        IsCompressed BIT NOT NULL DEFAULT 0,
        IsChecksumEnabled BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(MAX) NULL,
        CreatedBy NVARCHAR(128) NOT NULL DEFAULT SYSTEM_USER
    );

    CREATE INDEX IX_tbl_DatabaseBackupHistory_StartedOn ON dbo.tbl_DatabaseBackupHistory (StartedOn DESC);
    CREATE INDEX IX_tbl_DatabaseBackupHistory_Status ON dbo.tbl_DatabaseBackupHistory (Status);
END;
GO

IF OBJECT_ID(N'dbo.tbl_DatabaseIntegrityHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_DatabaseIntegrityHistory (
        IntegrityCheckId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DatabaseName NVARCHAR(128) NOT NULL,
        StartedOn DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CompletedOn DATETIME2 NULL,
        Status NVARCHAR(50) NOT NULL, -- Running, Passed, Failed
        ResultSummary NVARCHAR(MAX) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ExecutedBy NVARCHAR(128) NOT NULL DEFAULT SYSTEM_USER
    );

    CREATE INDEX IX_tbl_DatabaseIntegrityHistory_StartedOn ON dbo.tbl_DatabaseIntegrityHistory (StartedOn DESC);
END;
GO

IF OBJECT_ID(N'dbo.tbl_DatabaseBackupConfiguration', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_DatabaseBackupConfiguration (
        ConfigId INT PRIMARY KEY DEFAULT 1,
        PrimaryBackupPath NVARCHAR(512) NOT NULL DEFAULT 'C:\StaffAutomation\Backups',
        SecondaryBackupPath NVARCHAR(512) NULL,
        EnableSecondaryCopy BIT NOT NULL DEFAULT 0,
        RetentionDays INT NOT NULL DEFAULT 30,
        EnableAutomaticBackup BIT NOT NULL DEFAULT 1,
        ScheduledBackupTime NVARCHAR(10) NOT NULL DEFAULT '23:00',
        LastSuccessfulBackupOn DATETIME2 NULL,
        UpdatedOn DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT CK_tbl_DatabaseBackupConfiguration_SingleRow CHECK (ConfigId = 1)
    );

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DatabaseBackupConfiguration WHERE ConfigId = 1)
    BEGIN
        INSERT INTO dbo.tbl_DatabaseBackupConfiguration (ConfigId, PrimaryBackupPath, RetentionDays, EnableAutomaticBackup, ScheduledBackupTime)
        VALUES (1, 'C:\StaffAutomation\Backups', 30, 1, '23:00');
    END;
END;
GO
