-- ===============================================================================
-- Script ID      : 11_Phase11_Core_Configuration_And_Security_Tables.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+
-- Description    : Phase 11.1 Additive Idempotent DDL Migration
--                  1. dbo.tbl_FirmProfile (Singleton CA Firm Profile)
--                  2. dbo.tbl_Permissions (Central Permissions Catalog)
--                  3. dbo.tbl_RolePermissions (Role-Permission Matrix)
--                  4. dbo.tbl_UserPermissionOverrides (User Permission Overrides)
--                  5. Extend dbo.tbl_FinancialYears (IsLocked, IsDeleted, ModifiedOn, ModifiedBy)
-- Standards      : 100% Idempotent, Non-destructive, Parameterized/Safe Seeding
-- ===============================================================================

USE [StaffAutomationDb];
GO

-- -------------------------------------------------------------------------------
-- 1. Extend dbo.tbl_FinancialYears (Safely add columns if missing)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_FinancialYears') AND name = N'IsLocked')
BEGIN
    ALTER TABLE dbo.tbl_FinancialYears ADD IsLocked BIT NOT NULL CONSTRAINT DF_tbl_FY_IsLocked DEFAULT (0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_FinancialYears') AND name = N'IsDeleted')
BEGIN
    ALTER TABLE dbo.tbl_FinancialYears ADD IsDeleted BIT NOT NULL CONSTRAINT DF_tbl_FY_IsDeleted DEFAULT (0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_FinancialYears') AND name = N'ModifiedOn')
BEGIN
    ALTER TABLE dbo.tbl_FinancialYears ADD ModifiedOn DATETIME2 NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_FinancialYears') AND name = N'ModifiedBy')
BEGIN
    ALTER TABLE dbo.tbl_FinancialYears ADD ModifiedBy INT NULL;
END;
GO

-- -------------------------------------------------------------------------------
-- 2. dbo.tbl_FirmProfile (Singleton CA Firm Profile)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_FirmProfile', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_FirmProfile (
        FirmId INT IDENTITY(1,1) NOT NULL,
        FirmName NVARCHAR(150) NOT NULL,
        LegalName NVARCHAR(150) NOT NULL,
        FRN NVARCHAR(30) NOT NULL, -- Firm Registration Number
        PAN NVARCHAR(15) NOT NULL,
        GSTIN NVARCHAR(20) NULL,
        AddressLine1 NVARCHAR(200) NOT NULL,
        AddressLine2 NVARCHAR(200) NULL,
        City NVARCHAR(100) NOT NULL,
        StateName NVARCHAR(100) NOT NULL,
        Pincode NVARCHAR(10) NOT NULL,
        Phone NVARCHAR(20) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        Website NVARCHAR(150) NULL,
        HeaderFormatJson NVARCHAR(MAX) NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_FirmProfile_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_FirmProfile_CreatedBy DEFAULT (1),
        ModifiedOn DATETIME2 NULL,
        ModifiedBy INT NULL,
        CONSTRAINT PK_tbl_FirmProfile PRIMARY KEY CLUSTERED (FirmId ASC),
        CONSTRAINT CK_tbl_FirmProfile_SingleRow CHECK (FirmId = 1)
    );
END;
GO

-- Idempotent Seed Default Firm Profile (FirmId = 1)
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_FirmProfile WHERE FirmId = 1)
BEGIN
    INSERT INTO dbo.tbl_FirmProfile (
        FirmName, LegalName, FRN, PAN, GSTIN, AddressLine1, AddressLine2, City, StateName, Pincode, Phone, Email, Website, CreatedBy
    )
    VALUES (
        N'Demo CA & Associates',
        N'Demo CA & Associates Chartered Accountants',
        N'123456W',
        N'ABCDE1234F',
        N'27ABCDE1234F1Z5',
        N'101 Commercial Plaza, MG Road',
        N'Fort',
        N'Mumbai',
        N'Maharashtra',
        N'400001',
        N'022-22001122',
        N'info@democa.com',
        N'https://www.democa.com',
        1
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 3. dbo.tbl_Permissions (Central Permissions Catalog)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Permissions (
        PermissionId INT IDENTITY(1,1) NOT NULL,
        PermissionCode NVARCHAR(100) NOT NULL,
        PermissionName NVARCHAR(150) NOT NULL,
        ModuleName NVARCHAR(50) NOT NULL,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_Permissions_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Permissions_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_Permissions_CreatedBy DEFAULT (1),
        ModifiedOn DATETIME2 NULL,
        ModifiedBy INT NULL,
        CONSTRAINT PK_tbl_Permissions PRIMARY KEY CLUSTERED (PermissionId ASC),
        CONSTRAINT UQ_tbl_Permissions_Code UNIQUE NONCLUSTERED (PermissionCode ASC)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.tbl_Permissions') AND name = N'IX_tbl_Permissions_Module')
BEGIN
    CREATE INDEX IX_tbl_Permissions_Module ON dbo.tbl_Permissions (ModuleName ASC);
END;
GO

-- Idempotent Seed 25 Core Domain Permissions
MERGE INTO dbo.tbl_Permissions AS Target
USING (VALUES
    -- User Management Domain
    (N'USER_VIEW', N'View Staff Accounts', N'User Management', N'Ability to view staff user list and profiles'),
    (N'USER_CREATE', N'Create Staff Account', N'User Management', N'Ability to provision new staff user accounts'),
    (N'USER_EDIT', N'Edit Staff Account', N'User Management', N'Ability to update staff profiles and roles'),
    (N'USER_RESET_PASSWORD', N'Reset Staff Password', N'User Management', N'Ability to generate temporary password resets'),
    (N'USER_DEACTIVATE', N'Deactivate Account', N'User Management', N'Ability to disable active staff accounts'),
    
    -- Client Master Domain
    (N'CLIENT_VIEW', N'View Client Registry', N'Client Master', N'Ability to view client master directory'),
    (N'CLIENT_CREATE', N'Create Client Record', N'Client Master', N'Ability to register new clients'),
    (N'CLIENT_EDIT', N'Edit Client Profile', N'Client Master', N'Ability to update client GSTIN, PAN, and profiles'),
    (N'CLIENT_ARCHIVE', N'Archive Client', N'Client Master', N'Ability to soft-delete/archive client records'),
    
    -- Attendance Supervision Domain
    (N'ATTENDANCE_VIEW', N'View Attendance Logs', N'Attendance', N'Ability to view staff attendance records'),
    (N'ATTENDANCE_EDIT', N'Edit Attendance Log', N'Attendance', N'Ability to apply manual attendance corrections'),
    (N'ATTENDANCE_APPROVE', N'Approve Corrections', N'Attendance', N'Ability to approve administrative punch overrides'),
    
    -- Backup & Disaster Recovery Domain
    (N'BACKUP_VIEW', N'View Backup Health', N'Disaster Recovery', N'Ability to view database backup status & logs'),
    (N'BACKUP_CREATE', N'Execute T-SQL Backup', N'Disaster Recovery', N'Ability to trigger on-demand database backups'),
    (N'BACKUP_RESTORE', N'Execute DB Restore', N'Disaster Recovery', N'Ability to initiate database restorations'),
    (N'BACKUP_CONFIGURE', N'Configure Backup Storage', N'Disaster Recovery', N'Ability to configure storage paths and retention'),
    
    -- Firm Master Profile Domain
    (N'FIRM_PROFILE_VIEW', N'View Firm Profile', N'Firm Settings', N'Ability to view firm legal identity details'),
    (N'FIRM_PROFILE_EDIT', N'Edit Firm Profile', N'Firm Settings', N'Ability to update FRN, PAN, GSTIN & letterhead'),
    
    -- Financial Year Management Domain
    (N'FINANCIAL_YEAR_VIEW', N'View Accounting FY', N'Financial Year', N'Ability to view financial year periods'),
    (N'FINANCIAL_YEAR_MANAGE', N'Manage Accounting FY', N'Financial Year', N'Ability to create and activate financial years'),
    (N'FINANCIAL_YEAR_LOCK', N'Lock Completed FY', N'Financial Year', N'Ability to lock completed financial years'),
    
    -- Role & Security Management Domain
    (N'ROLE_SECURITY_VIEW', N'View Security Roles', N'Role Security', N'Ability to view role permission matrix'),
    (N'ROLE_SECURITY_MANAGE', N'Manage Role Permissions', N'Role Security', N'Ability to assign permissions to roles'),
    
    -- Compliance Audit Domain
    (N'AUDIT_LOG_VIEW', N'View Audit Logs', N'Compliance Audit', N'Ability to query system security audit trail'),
    (N'AUDIT_LOG_EXPORT', N'Export Audit Logs', N'Compliance Audit', N'Ability to export tamper-proof audit trails')
) AS Source (PermissionCode, PermissionName, ModuleName, Description)
ON Target.PermissionCode = Source.PermissionCode
WHEN NOT MATCHED THEN
    INSERT (PermissionCode, PermissionName, ModuleName, Description, CreatedBy)
    VALUES (Source.PermissionCode, Source.PermissionName, Source.ModuleName, Source.Description, 1);
GO

-- -------------------------------------------------------------------------------
-- 4. dbo.tbl_RolePermissions (Role-to-Permission Matrix)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_RolePermissions (
        RolePermissionId INT IDENTITY(1,1) NOT NULL,
        RoleId INT NOT NULL,
        PermissionId INT NOT NULL,
        GrantedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_RolePermissions_GrantedOn DEFAULT (GETUTCDATE()),
        GrantedBy INT NOT NULL CONSTRAINT DF_tbl_RolePermissions_GrantedBy DEFAULT (1),
        CONSTRAINT PK_tbl_RolePermissions PRIMARY KEY CLUSTERED (RolePermissionId ASC),
        CONSTRAINT FK_tbl_RolePermissions_tbl_Roles FOREIGN KEY (RoleId) REFERENCES dbo.tbl_Roles (RoleId),
        CONSTRAINT FK_tbl_RolePermissions_tbl_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.tbl_Permissions (PermissionId),
        CONSTRAINT UQ_tbl_RolePermissions_RolePerm UNIQUE NONCLUSTERED (RoleId ASC, PermissionId ASC)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.tbl_RolePermissions') AND name = N'IX_tbl_RolePermissions_Lookup')
BEGIN
    CREATE INDEX IX_tbl_RolePermissions_Lookup ON dbo.tbl_RolePermissions (RoleId ASC, PermissionId ASC);
END;
GO

-- Idempotent Seed: Grant all catalog permissions to Admin role dynamically via tbl_Roles lookup
DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = RoleId FROM dbo.tbl_Roles WHERE RoleName = N'Admin';

IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId, GrantedBy)
    SELECT @AdminRoleId, p.PermissionId, 1
    FROM dbo.tbl_Permissions p
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.PermissionId
    );
END;
GO

-- Idempotent Seed: Grant standard permissions to Owner role dynamically
DECLARE @OwnerRoleId INT;
SELECT @OwnerRoleId = RoleId FROM dbo.tbl_Roles WHERE RoleName = N'Owner';

IF @OwnerRoleId IS NOT NULL
BEGIN
    INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId, GrantedBy)
    SELECT @OwnerRoleId, p.PermissionId, 1
    FROM dbo.tbl_Permissions p
    WHERE p.PermissionCode IN (
        N'USER_VIEW', N'CLIENT_VIEW', N'CLIENT_CREATE', N'CLIENT_EDIT', N'CLIENT_ARCHIVE',
        N'ATTENDANCE_VIEW', N'ATTENDANCE_EDIT', N'ATTENDANCE_APPROVE', N'BACKUP_VIEW',
        N'FIRM_PROFILE_VIEW', N'FINANCIAL_YEAR_VIEW', N'FINANCIAL_YEAR_MANAGE',
        N'AUDIT_LOG_VIEW', N'AUDIT_LOG_EXPORT'
    )
    AND NOT EXISTS (
        SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = @OwnerRoleId AND rp.PermissionId = p.PermissionId
    );
END;
GO

-- Idempotent Seed: Grant basic staff permissions to Employee role dynamically
DECLARE @EmployeeRoleId INT;
SELECT @EmployeeRoleId = RoleId FROM dbo.tbl_Roles WHERE RoleName = N'Employee';

IF @EmployeeRoleId IS NOT NULL
BEGIN
    INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId, GrantedBy)
    SELECT @EmployeeRoleId, p.PermissionId, 1
    FROM dbo.tbl_Permissions p
    WHERE p.PermissionCode IN (
        N'CLIENT_VIEW', N'ATTENDANCE_VIEW', N'FIRM_PROFILE_VIEW', N'FINANCIAL_YEAR_VIEW'
    )
    AND NOT EXISTS (
        SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = @EmployeeRoleId AND rp.PermissionId = p.PermissionId
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 5. dbo.tbl_UserPermissionOverrides (User Permission Overrides)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_UserPermissionOverrides', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_UserPermissionOverrides (
        OverrideId INT IDENTITY(1,1) NOT NULL,
        UserId INT NOT NULL,
        PermissionId INT NOT NULL,
        IsGranted BIT NOT NULL, -- 1 = Explicit Grant, 0 = Explicit Deny
        Reason NVARCHAR(250) NULL,
        GrantedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_UserOverrides_GrantedOn DEFAULT (GETUTCDATE()),
        GrantedBy INT NOT NULL CONSTRAINT DF_tbl_UserOverrides_GrantedBy DEFAULT (1),
        CONSTRAINT PK_tbl_UserPermissionOverrides PRIMARY KEY CLUSTERED (OverrideId ASC),
        CONSTRAINT FK_tbl_UserOverrides_tbl_Users FOREIGN KEY (UserId) REFERENCES dbo.tbl_Users (UserId),
        CONSTRAINT FK_tbl_UserOverrides_tbl_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.tbl_Permissions (PermissionId),
        CONSTRAINT UQ_tbl_UserOverrides_UserPerm UNIQUE NONCLUSTERED (UserId ASC, PermissionId ASC)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.tbl_UserPermissionOverrides') AND name = N'IX_tbl_UserOverrides_Lookup')
BEGIN
    CREATE INDEX IX_tbl_UserOverrides_Lookup ON dbo.tbl_UserPermissionOverrides (UserId ASC, PermissionId ASC);
END;
GO

-- -------------------------------------------------------------------------------
-- 6. dbo.tbl_DatabaseBackupHistory (Backup Audit History)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 7. dbo.tbl_DatabaseIntegrityHistory (DBCC Integrity Audit History)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 8. dbo.tbl_DatabaseBackupConfiguration (Disaster Recovery Configuration)
-- -------------------------------------------------------------------------------
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

    INSERT INTO dbo.tbl_DatabaseBackupConfiguration (ConfigId, PrimaryBackupPath, RetentionDays, EnableAutomaticBackup, ScheduledBackupTime)
    VALUES (1, 'C:\StaffAutomation\Backups', 30, 1, '23:00');
END;
GO
