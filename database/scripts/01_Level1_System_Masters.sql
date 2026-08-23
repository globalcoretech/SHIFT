-- ===============================================================================
-- Script ID      : 01_Level1_System_Masters.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+
-- Description    : Level 1 System Masters DDL (Roles, Departments, Statuses, Priorities, Comm Types, Comm Outcomes, Time Categories, System Settings)
-- Standards      : Strict PK IDENTITY(1,1), UTC Timestamps, Soft Deletes (IsDeleted)
-- ===============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'StaffAutomationDb')
BEGIN
    CREATE DATABASE [StaffAutomationDb];
END
GO

USE [StaffAutomationDb];
GO

-- -------------------------------------------------------------------------------
-- 1. tbl_Roles (Configurable User Access Roles Master)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Roles (
        RoleId INT IDENTITY(1,1) NOT NULL,
        RoleName NVARCHAR(50) NOT NULL,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_Roles_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Roles_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_Roles_CreatedBy DEFAULT (1),
        ModifiedOn DATETIME2 NULL,
        ModifiedBy INT NULL,
        CONSTRAINT PK_tbl_Roles PRIMARY KEY CLUSTERED (RoleId ASC),
        CONSTRAINT UQ_tbl_Roles_RoleName UNIQUE NONCLUSTERED (RoleName ASC)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 2. tbl_Departments (Department Classification Master)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Departments (
        DepartmentId INT IDENTITY(1,1) NOT NULL,
        DepartmentCode NVARCHAR(20) NOT NULL,
        DepartmentName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_Departments_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Departments_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_Departments_CreatedBy DEFAULT (1),
        ModifiedOn DATETIME2 NULL,
        ModifiedBy INT NULL,
        CONSTRAINT PK_tbl_Departments PRIMARY KEY CLUSTERED (DepartmentId ASC),
        CONSTRAINT UQ_tbl_Departments_Code UNIQUE NONCLUSTERED (DepartmentCode ASC)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 3. tbl_TaskStatuses (11-State Task Workflow State Machine Master)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_TaskStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TaskStatuses (
        StatusId INT IDENTITY(1,1) NOT NULL,
        StatusCode NVARCHAR(30) NOT NULL,
        StatusName NVARCHAR(50) NOT NULL,
        DisplayOrder INT NOT NULL,
        IsTerminalState BIT NOT NULL CONSTRAINT DF_tbl_TaskStatuses_IsTerminal DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_TaskStatuses_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_TaskStatuses_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_TaskStatuses PRIMARY KEY CLUSTERED (StatusId ASC),
        CONSTRAINT UQ_tbl_TaskStatuses_StatusCode UNIQUE NONCLUSTERED (StatusCode ASC)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 4. tbl_TaskPriorities (Task Priority Classification Master)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_TaskPriorities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TaskPriorities (
        PriorityId INT IDENTITY(1,1) NOT NULL,
        PriorityCode NVARCHAR(20) NOT NULL,
        PriorityName NVARCHAR(50) NOT NULL,
        ColorHexCode NVARCHAR(10) NOT NULL CONSTRAINT DF_tbl_TaskPriorities_Color DEFAULT (N'#808080'),
        SeverityRank INT NOT NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_TaskPriorities_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_TaskPriorities_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_TaskPriorities PRIMARY KEY CLUSTERED (PriorityId ASC),
        CONSTRAINT UQ_tbl_TaskPriorities_Code UNIQUE NONCLUSTERED (PriorityCode ASC)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 5. tbl_CommunicationTypes (Client Communication Channel Master)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_CommunicationTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_CommunicationTypes (
        TypeId INT IDENTITY(1,1) NOT NULL,
        TypeCode NVARCHAR(30) NOT NULL,
        TypeName NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_CommTypes_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_CommTypes_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_CommTypes_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_CommunicationTypes PRIMARY KEY CLUSTERED (TypeId ASC),
        CONSTRAINT UQ_tbl_CommTypes_Code UNIQUE NONCLUSTERED (TypeCode ASC)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 6. tbl_CommunicationOutcomes (Client Discussion Outcome Master)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_CommunicationOutcomes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_CommunicationOutcomes (
        OutcomeId INT IDENTITY(1,1) NOT NULL,
        OutcomeCode NVARCHAR(30) NOT NULL,
        OutcomeName NVARCHAR(100) NOT NULL,
        RequiresFollowUp BIT NOT NULL CONSTRAINT DF_tbl_CommOutcomes_FollowUp DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_CommOutcomes_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_CommOutcomes_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_CommunicationOutcomes PRIMARY KEY CLUSTERED (OutcomeId ASC),
        CONSTRAINT UQ_tbl_CommOutcomes_Code UNIQUE NONCLUSTERED (OutcomeCode ASC)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 7. tbl_TimeCategories (Productivity Time Classification Master)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_TimeCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TimeCategories (
        CategoryId INT IDENTITY(1,1) NOT NULL,
        CategoryCode NVARCHAR(30) NOT NULL,
        CategoryName NVARCHAR(50) NOT NULL,
        IsBillable BIT NOT NULL CONSTRAINT DF_tbl_TimeCategories_IsBillable DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_TimeCategories_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_TimeCategories_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_TimeCategories PRIMARY KEY CLUSTERED (CategoryId ASC),
        CONSTRAINT UQ_tbl_TimeCategories_Code UNIQUE NONCLUSTERED (CategoryCode ASC)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- 8. tbl_SystemSettings (Configurable Key-Value Setting Store)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_SystemSettings (
        SettingId INT IDENTITY(1,1) NOT NULL,
        SettingKey NVARCHAR(100) NOT NULL,
        SettingValue NVARCHAR(500) NOT NULL,
        Description NVARCHAR(250) NULL,
        ModifiedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_SystemSettings_ModifiedOn DEFAULT (GETUTCDATE()),
        ModifiedBy INT NOT NULL CONSTRAINT DF_tbl_SystemSettings_ModifiedBy DEFAULT (1),
        CONSTRAINT PK_tbl_SystemSettings PRIMARY KEY CLUSTERED (SettingId ASC),
        CONSTRAINT UQ_tbl_SystemSettings_Key UNIQUE NONCLUSTERED (SettingKey ASC)
    );
END;
GO
