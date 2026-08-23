-- ===============================================================================
-- Script ID      : 00_Master_Complete_Schema_And_Seed.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+
-- Description    : All-In-One Master Migration & Seed Script (Creates DB, 21 Tables, 8 Views, Indexes & Seed Data)
-- ===============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'StaffAutomationDb')
BEGIN
    CREATE DATABASE [StaffAutomationDb];
END;
GO

USE [StaffAutomationDb];
GO

-- -------------------------------------------------------------------------------
-- LEVEL 1: System Master Tables
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

-- -------------------------------------------------------------------------------
-- LEVEL 2: Business Master Tables
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Users (
        UserId INT IDENTITY(1,1) NOT NULL,
        Username NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(256) NOT NULL,
        PasswordSalt NVARCHAR(128) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        RoleId INT NOT NULL,
        DepartmentId INT NOT NULL,
        Email NVARCHAR(100) NULL,
        Phone NVARCHAR(20) NULL,
        MustChangePassword BIT NOT NULL CONSTRAINT DF_tbl_Users_MustChangePassword DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_Users_IsActive DEFAULT (1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_tbl_Users_IsDeleted DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Users_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_Users_CreatedBy DEFAULT (1),
        ModifiedOn DATETIME2 NULL,
        ModifiedBy INT NULL,
        CONSTRAINT PK_tbl_Users PRIMARY KEY CLUSTERED (UserId ASC),
        CONSTRAINT UQ_tbl_Users_Username UNIQUE NONCLUSTERED (Username ASC),
        CONSTRAINT FK_tbl_Users_tbl_Roles FOREIGN KEY (RoleId) REFERENCES dbo.tbl_Roles (RoleId),
        CONSTRAINT FK_tbl_Users_tbl_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments (DepartmentId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_ClientCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ClientCategories (
        ClientCategoryId INT IDENTITY(1,1) NOT NULL,
        CategoryCode NVARCHAR(30) NOT NULL,
        CategoryName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_ClientCat_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_ClientCat_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_ClientCat_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_ClientCategories PRIMARY KEY CLUSTERED (ClientCategoryId ASC),
        CONSTRAINT UQ_tbl_ClientCat_Code UNIQUE NONCLUSTERED (CategoryCode ASC)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_Clients', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Clients (
        ClientId INT IDENTITY(1,1) NOT NULL,
        ClientCode NVARCHAR(30) NOT NULL,
        ClientName NVARCHAR(150) NOT NULL,
        ClientCategoryId INT NOT NULL,
        ContactPerson NVARCHAR(100) NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        PAN NVARCHAR(15) NULL,
        GSTIN NVARCHAR(20) NULL,
        DepartmentId INT NOT NULL,
        StateName NVARCHAR(100) NULL,
        District NVARCHAR(100) NULL,
        Pincode NVARCHAR(20) NULL,
        Address NVARCHAR(MAX) NULL,
        EntityType NVARCHAR(100) NULL,
        GstType NVARCHAR(100) NULL,
        IsLiveApiData BIT NOT NULL CONSTRAINT DF_tbl_Clients_IsLiveApiData DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_Clients_IsActive DEFAULT (1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_tbl_Clients_IsDeleted DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Clients_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_Clients_CreatedBy DEFAULT (1),
        ModifiedOn DATETIME2 NULL,
        ModifiedBy INT NULL,
        CONSTRAINT PK_tbl_Clients PRIMARY KEY CLUSTERED (ClientId ASC),
        CONSTRAINT UQ_tbl_Clients_ClientCode UNIQUE NONCLUSTERED (ClientCode ASC),
        CONSTRAINT FK_tbl_Clients_tbl_ClientCategories FOREIGN KEY (ClientCategoryId) REFERENCES dbo.tbl_ClientCategories (ClientCategoryId),
        CONSTRAINT FK_tbl_Clients_tbl_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments (DepartmentId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_FinancialYears', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_FinancialYears (
        FinancialYearId INT IDENTITY(1,1) NOT NULL,
        FYCode NVARCHAR(20) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        IsCurrentFY BIT NOT NULL CONSTRAINT DF_tbl_FY_IsCurrent DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_FY_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_FY_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_FinancialYears PRIMARY KEY CLUSTERED (FinancialYearId ASC),
        CONSTRAINT UQ_tbl_FY_Code UNIQUE NONCLUSTERED (FYCode ASC)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_DocumentTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_DocumentTypes (
        DocumentTypeId INT IDENTITY(1,1) NOT NULL,
        TypeCode NVARCHAR(30) NOT NULL,
        TypeName NVARCHAR(100) NOT NULL,
        DepartmentId INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_DocTypes_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_DocTypes_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_DocTypes_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_DocumentTypes PRIMARY KEY CLUSTERED (DocumentTypeId ASC),
        CONSTRAINT UQ_tbl_DocTypes_Code UNIQUE NONCLUSTERED (TypeCode ASC),
        CONSTRAINT FK_tbl_DocTypes_tbl_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments (DepartmentId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_TaskCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TaskCategories (
        CategoryId INT IDENTITY(1,1) NOT NULL,
        CategoryCode NVARCHAR(30) NOT NULL,
        CategoryName NVARCHAR(100) NOT NULL,
        DepartmentId INT NOT NULL,
        EstimatedHours DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_TaskCategories_EstHours DEFAULT (1.00),
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_TaskCategories_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_TaskCategories_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_TaskCategories_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_TaskCategories PRIMARY KEY CLUSTERED (CategoryId ASC),
        CONSTRAINT UQ_tbl_TaskCategories_Code UNIQUE NONCLUSTERED (CategoryCode ASC),
        CONSTRAINT FK_tbl_TaskCategories_tbl_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments (DepartmentId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_TaskTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TaskTemplates (
        TemplateId INT IDENTITY(1,1) NOT NULL,
        TemplateName NVARCHAR(150) NOT NULL,
        CategoryId INT NOT NULL,
        DefaultPriorityId INT NOT NULL,
        StandardFilingDaysOffset INT NOT NULL CONSTRAINT DF_tbl_TaskTemplates_Offset DEFAULT (30),
        Description NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tbl_TaskTemplates_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_TaskTemplates_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL CONSTRAINT DF_tbl_TaskTemplates_CreatedBy DEFAULT (1),
        CONSTRAINT PK_tbl_TaskTemplates PRIMARY KEY CLUSTERED (TemplateId ASC),
        CONSTRAINT FK_tbl_TaskTemplates_tbl_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.tbl_TaskCategories (CategoryId),
        CONSTRAINT FK_tbl_TaskTemplates_tbl_Priorities FOREIGN KEY (DefaultPriorityId) REFERENCES dbo.tbl_TaskPriorities (PriorityId)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- LEVEL 3: Transactional Operational Tables
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.tbl_Tasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Tasks (
        TaskId INT IDENTITY(1,1) NOT NULL,
        TaskCode NVARCHAR(30) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        ClientId INT NOT NULL,
        CategoryId INT NOT NULL,
        FinancialYearId INT NOT NULL,
        AssignedToUserId INT NOT NULL,
        AssignedByUserId INT NOT NULL,
        DepartmentId INT NOT NULL,
        PriorityId INT NOT NULL,
        StatusId INT NOT NULL,
        AssignmentDate DATETIME2 NOT NULL CONSTRAINT DF_tbl_Tasks_AssignDate DEFAULT (GETUTCDATE()),
        TargetDueDate DATETIME2 NOT NULL,
        ReminderDate DATETIME2 NULL,
        CompletionDate DATETIME2 NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_tbl_Tasks_IsDeleted DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Tasks_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL,
        ModifiedOn DATETIME2 NULL,
        ModifiedBy INT NULL,
        CONSTRAINT PK_tbl_Tasks PRIMARY KEY CLUSTERED (TaskId ASC),
        CONSTRAINT UQ_tbl_Tasks_TaskCode UNIQUE NONCLUSTERED (TaskCode ASC),
        CONSTRAINT FK_tbl_Tasks_tbl_Clients FOREIGN KEY (ClientId) REFERENCES dbo.tbl_Clients (ClientId),
        CONSTRAINT FK_tbl_Tasks_tbl_TaskCategories FOREIGN KEY (CategoryId) REFERENCES dbo.tbl_TaskCategories (CategoryId),
        CONSTRAINT FK_tbl_Tasks_tbl_FinancialYears FOREIGN KEY (FinancialYearId) REFERENCES dbo.tbl_FinancialYears (FinancialYearId),
        CONSTRAINT FK_tbl_Tasks_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES dbo.tbl_Users (UserId),
        CONSTRAINT FK_tbl_Tasks_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES dbo.tbl_Users (UserId),
        CONSTRAINT FK_tbl_Tasks_tbl_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments (DepartmentId),
        CONSTRAINT FK_tbl_Tasks_tbl_TaskPriorities FOREIGN KEY (PriorityId) REFERENCES dbo.tbl_TaskPriorities (PriorityId),
        CONSTRAINT FK_tbl_Tasks_tbl_TaskStatuses FOREIGN KEY (StatusId) REFERENCES dbo.tbl_TaskStatuses (StatusId),
        CONSTRAINT CK_tbl_Tasks_DueDate CHECK (TargetDueDate >= AssignmentDate)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_TaskActivities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TaskActivities (
        ActivityId INT IDENTITY(1,1) NOT NULL,
        TaskId INT NOT NULL,
        UserId INT NOT NULL,
        TimeCategoryId INT NOT NULL,
        ActivityDescription NVARCHAR(500) NOT NULL,
        StartTime DATETIME2 NOT NULL,
        EndTime DATETIME2 NULL,
        DurationMinutes INT NOT NULL CONSTRAINT DF_tbl_TaskActivities_Duration DEFAULT (0),
        ActivityDate DATE NOT NULL CONSTRAINT DF_tbl_TaskActivities_ActDate DEFAULT (CAST(GETUTCDATE() AS DATE)),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_TaskActivities_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL,
        CONSTRAINT PK_tbl_TaskActivities PRIMARY KEY CLUSTERED (ActivityId ASC),
        CONSTRAINT FK_tbl_TaskActivities_tbl_Tasks FOREIGN KEY (TaskId) REFERENCES dbo.tbl_Tasks (TaskId),
        CONSTRAINT FK_tbl_TaskActivities_tbl_Users FOREIGN KEY (UserId) REFERENCES dbo.tbl_Users (UserId),
        CONSTRAINT FK_tbl_TaskActivities_tbl_TimeCategories FOREIGN KEY (TimeCategoryId) REFERENCES dbo.tbl_TimeCategories (CategoryId),
        CONSTRAINT CK_tbl_TaskActivities_EndTime CHECK (EndTime IS NULL OR EndTime >= StartTime)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_Discussions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Discussions (
        DiscussionId INT IDENTITY(1,1) NOT NULL,
        ClientId INT NOT NULL,
        TaskId INT NULL,
        UserId INT NOT NULL,
        CommunicationTypeId INT NOT NULL,
        OutcomeId INT NOT NULL,
        DiscussionNotes NVARCHAR(MAX) NOT NULL,
        DurationMinutes INT NOT NULL CONSTRAINT DF_tbl_Discussions_Duration DEFAULT (0),
        DiscussionTimestamp DATETIME2 NOT NULL CONSTRAINT DF_tbl_Discussions_Timestamp DEFAULT (GETUTCDATE()),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Discussions_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL,
        CONSTRAINT PK_tbl_Discussions PRIMARY KEY CLUSTERED (DiscussionId ASC),
        CONSTRAINT FK_tbl_Discussions_tbl_Clients FOREIGN KEY (ClientId) REFERENCES dbo.tbl_Clients (ClientId),
        CONSTRAINT FK_tbl_Discussions_tbl_Tasks FOREIGN KEY (TaskId) REFERENCES dbo.tbl_Tasks (TaskId),
        CONSTRAINT FK_tbl_Discussions_tbl_Users FOREIGN KEY (UserId) REFERENCES dbo.tbl_Users (UserId),
        CONSTRAINT FK_tbl_Discussions_tbl_CommTypes FOREIGN KEY (CommunicationTypeId) REFERENCES dbo.tbl_CommunicationTypes (TypeId),
        CONSTRAINT FK_tbl_Discussions_tbl_CommOutcomes FOREIGN KEY (OutcomeId) REFERENCES dbo.tbl_CommunicationOutcomes (OutcomeId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_Attendance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Attendance (
        AttendanceId INT IDENTITY(1,1) NOT NULL,
        UserId INT NOT NULL,
        AttendanceDate DATE NOT NULL CONSTRAINT DF_tbl_Attendance_Date DEFAULT (CAST(GETUTCDATE() AS DATE)),
        ClockInTime DATETIME2 NOT NULL,
        ClockOutTime DATETIME2 NULL,
        BreakStartTime DATETIME2 NULL,
        TotalBreakMinutes INT NOT NULL CONSTRAINT DF_tbl_Attendance_Break DEFAULT (0),
        TotalWorkingMinutes INT NOT NULL CONSTRAINT DF_tbl_Attendance_WorkMin DEFAULT (0),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_tbl_Attendance_Status DEFAULT (N'Present'),
        ClientIP NVARCHAR(45) NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Attendance_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL,
        CONSTRAINT PK_tbl_Attendance PRIMARY KEY CLUSTERED (AttendanceId ASC),
        CONSTRAINT FK_tbl_Attendance_tbl_Users FOREIGN KEY (UserId) REFERENCES dbo.tbl_Users (UserId),
        CONSTRAINT UQ_tbl_Attendance_UserDate UNIQUE NONCLUSTERED (UserId ASC, AttendanceDate ASC),
        CONSTRAINT CK_tbl_Attendance_ClockOut CHECK (ClockOutTime IS NULL OR ClockOutTime >= ClockInTime)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Notifications (
        NotificationId INT IDENTITY(1,1) NOT NULL,
        RecipientUserId INT NOT NULL,
        Title NVARCHAR(100) NOT NULL,
        Message NVARCHAR(500) NOT NULL,
        NotificationType NVARCHAR(30) NOT NULL CONSTRAINT DF_tbl_Notifications_Type DEFAULT (N'INFO'),
        IsRead BIT NOT NULL CONSTRAINT DF_tbl_Notifications_IsRead DEFAULT (0),
        ReadTimestamp DATETIME2 NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_tbl_Notifications_CreatedOn DEFAULT (GETUTCDATE()),
        CreatedBy INT NOT NULL,
        CONSTRAINT PK_tbl_Notifications PRIMARY KEY CLUSTERED (NotificationId ASC),
        CONSTRAINT FK_tbl_Notifications_tbl_Users FOREIGN KEY (RecipientUserId) REFERENCES dbo.tbl_Users (UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tbl_AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_AuditLogs (
        AuditId INT IDENTITY(1,1) NOT NULL,
        UserId INT NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        ModuleName NVARCHAR(50) NOT NULL,
        Details NVARCHAR(MAX) NOT NULL,
        IPAddress NVARCHAR(45) NULL,
        Timestamp DATETIME2 NOT NULL CONSTRAINT DF_tbl_AuditLogs_Timestamp DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_tbl_AuditLogs PRIMARY KEY CLUSTERED (AuditId ASC),
        CONSTRAINT FK_tbl_AuditLogs_tbl_Users FOREIGN KEY (UserId) REFERENCES dbo.tbl_Users (UserId)
    );
END;
GO

-- -------------------------------------------------------------------------------
-- Views & Compatibility Layer
-- -------------------------------------------------------------------------------
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.vw_LiveOwnerDashboard') IS NOT NULL DROP VIEW dbo.vw_LiveOwnerDashboard;
GO
CREATE VIEW dbo.vw_LiveOwnerDashboard
AS
SELECT 
    (SELECT COUNT(1) FROM dbo.tbl_Tasks t JOIN dbo.tbl_TaskStatuses s ON t.StatusId = s.StatusId WHERE s.StatusCode IN ('Assigned', 'InProgress', 'UnderReview') AND t.IsDeleted = 0) AS ActiveTasksCount,
    (SELECT COUNT(1) FROM dbo.tbl_Tasks t JOIN dbo.tbl_TaskStatuses s ON t.StatusId = s.StatusId WHERE s.StatusCode IN ('NewTask', 'OnHold') AND t.IsDeleted = 0) AS PendingTasksCount,
    (SELECT COUNT(1) FROM dbo.tbl_Tasks t JOIN dbo.tbl_TaskStatuses s ON t.StatusId = s.StatusId WHERE s.StatusCode = 'Completed' AND CAST(t.CompletionDate AS DATE) = CAST(GETUTCDATE() AS DATE) AND t.IsDeleted = 0) AS CompletedTodayCount,
    (SELECT COUNT(1) FROM dbo.tbl_Tasks t JOIN dbo.tbl_TaskStatuses s ON t.StatusId = s.StatusId WHERE t.TargetDueDate < GETUTCDATE() AND s.StatusCode NOT IN ('Completed', 'Delivered', 'Closed', 'Cancelled') AND t.IsDeleted = 0) AS OverdueTasksCount,
    (SELECT COUNT(1) FROM dbo.tbl_Tasks t JOIN dbo.tbl_TaskStatuses s ON t.StatusId = s.StatusId WHERE s.StatusCode = 'WaitingForClient' AND t.IsDeleted = 0) AS WaitingForClientCount,
    (SELECT COUNT(1) FROM dbo.tbl_Tasks t JOIN dbo.tbl_TaskStatuses s ON t.StatusId = s.StatusId WHERE s.StatusCode = 'WaitingForDocuments' AND t.IsDeleted = 0) AS WaitingForDocumentsCount,
    CAST(GETUTCDATE() AS DATETIME2) AS LastRefreshedTimestamp;
GO

IF OBJECT_ID(N'dbo.vw_MonthlyEmployeeProductivity') IS NOT NULL DROP VIEW dbo.vw_MonthlyEmployeeProductivity;
GO
CREATE VIEW dbo.vw_MonthlyEmployeeProductivity
AS
SELECT 
    u.UserId,
    u.FullName AS EmployeeName,
    d.DepartmentName,
    YEAR(a.ActivityDate) AS ActivityYear,
    MONTH(a.ActivityDate) AS ActivityMonth,
    SUM(CASE WHEN tc.CategoryCode = 'WorkTime' THEN a.DurationMinutes ELSE 0 END) AS WorkTimeMinutes,
    SUM(CASE WHEN tc.CategoryCode = 'DiscussionTime' THEN a.DurationMinutes ELSE 0 END) AS DiscussionTimeMinutes,
    SUM(CASE WHEN tc.CategoryCode = 'WaitingTime' THEN a.DurationMinutes ELSE 0 END) AS WaitingTimeMinutes,
    SUM(a.DurationMinutes) AS TotalLoggedMinutes
FROM dbo.tbl_Users u
JOIN dbo.tbl_Departments d ON u.DepartmentId = d.DepartmentId
JOIN dbo.tbl_TaskActivities a ON u.UserId = a.UserId
JOIN dbo.tbl_TimeCategories tc ON a.TimeCategoryId = tc.CategoryId
WHERE u.IsDeleted = 0
GROUP BY u.UserId, u.FullName, d.DepartmentName, YEAR(a.ActivityDate), MONTH(a.ActivityDate);
GO

IF OBJECT_ID(N'dbo.vw_Client360History') IS NOT NULL DROP VIEW dbo.vw_Client360History;
GO
CREATE VIEW dbo.vw_Client360History
AS
SELECT 
    c.ClientId,
    c.ClientCode,
    c.ClientName,
    t.TaskId,
    t.TaskCode,
    t.Title AS TaskTitle,
    s.StatusName AS TaskStatus,
    p.PriorityName AS TaskPriority,
    u.FullName AS AssignedEmployee,
    t.AssignmentDate,
    t.TargetDueDate,
    t.CompletionDate
FROM dbo.tbl_Clients c
JOIN dbo.tbl_Tasks t ON c.ClientId = t.ClientId
LEFT JOIN dbo.tbl_TaskStatuses s ON t.StatusId = s.StatusId
LEFT JOIN dbo.tbl_TaskPriorities p ON t.PriorityId = p.PriorityId
LEFT JOIN dbo.tbl_Users u ON t.AssignedToUserId = u.UserId
WHERE c.IsDeleted = 0 AND t.IsDeleted = 0;
GO

IF OBJECT_ID(N'dbo.tbl_User') IS NOT NULL DROP VIEW dbo.tbl_User;
GO
CREATE VIEW dbo.tbl_User AS SELECT * FROM dbo.tbl_Users;
GO

IF OBJECT_ID(N'dbo.tbl_Client') IS NOT NULL DROP VIEW dbo.tbl_Client;
GO
CREATE VIEW dbo.tbl_Client AS SELECT * FROM dbo.tbl_Clients;
GO

IF OBJECT_ID(N'dbo.tbl_Task') IS NOT NULL DROP VIEW dbo.tbl_Task;
GO
CREATE VIEW dbo.tbl_Task AS SELECT * FROM dbo.tbl_Tasks;
GO

IF OBJECT_ID(N'dbo.tbl_Discussion') IS NOT NULL DROP VIEW dbo.tbl_Discussion;
GO
CREATE VIEW dbo.tbl_Discussion AS SELECT * FROM dbo.tbl_Discussions;
GO

IF OBJECT_ID(N'dbo.tbl_TaskActivity') IS NOT NULL DROP VIEW dbo.tbl_TaskActivity;
GO
CREATE VIEW dbo.tbl_TaskActivity AS SELECT * FROM dbo.tbl_TaskActivities;
GO

-- -------------------------------------------------------------------------------
-- Seed Initial Master Data & Admin Accounts
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Employee')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, CreatedBy) VALUES ('Employee', 'Staff member executing daily tasks and activity timeline', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Owner')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, CreatedBy) VALUES ('Owner', 'CA Firm Owner/Partner with executive dashboard & report access', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Admin')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, CreatedBy) VALUES ('Admin', 'System administrator maintaining master catalogs and audit logs', 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, CreatedBy) VALUES ('ACCT', 'Accounting & Statutory Audit', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, CreatedBy) VALUES ('TAX', 'Income Tax & Indirect Tax (GST)', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'ADMIN')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, CreatedBy) VALUES ('ADMIN', 'Firm Administration & Secretarial', 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'NewTask')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES 
    ('NewTask', 'New', 1, 0, 1),
    ('Assigned', 'Assigned', 2, 0, 1),
    ('InProgress', 'In Progress', 3, 0, 1),
    ('WaitingForClient', 'Waiting for Client', 4, 0, 1),
    ('WaitingForDocuments', 'Waiting for Documents', 5, 0, 1),
    ('OnHold', 'On Hold', 6, 0, 1),
    ('UnderReview', 'Under Review', 7, 0, 1),
    ('Completed', 'Completed', 8, 0, 1),
    ('Delivered', 'Delivered', 9, 0, 1),
    ('Closed', 'Closed', 10, 1, 1),
    ('Cancelled', 'Cancelled', 11, 1, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskPriorities WHERE PriorityCode = 'LOW')
    INSERT INTO dbo.tbl_TaskPriorities (PriorityCode, PriorityName, ColorHexCode, SeverityRank, CreatedBy) VALUES 
    ('LOW', 'Low', '#28A745', 1, 1),
    ('MEDIUM', 'Medium', '#17A2B8', 2, 1),
    ('HIGH', 'High', '#FFC107', 3, 1),
    ('URGENT', 'Urgent', '#DC3545', 4, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TimeCategories WHERE CategoryCode = 'WorkTime')
    INSERT INTO dbo.tbl_TimeCategories (CategoryCode, CategoryName, IsBillable, CreatedBy) VALUES 
    ('WorkTime', 'Work Execution Time', 1, 1),
    ('DiscussionTime', 'Client Discussion Time', 1, 1),
    ('WaitingTime', 'Waiting Time (Client/Docs)', 0, 1),
    ('BreakTime', 'Break / Non-Billable Time', 0, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SystemSettings WHERE SettingKey = 'AutoSaveIntervalSeconds')
    INSERT INTO dbo.tbl_SystemSettings (SettingKey, SettingValue, Description, ModifiedBy) VALUES 
    ('AutoSaveIntervalSeconds', '60', 'Interval in seconds for auto-saving active timer drafts', 1),
    ('MaxDashboardRefreshMs', '500', 'Performance ceiling for live owner dashboard refresh', 1),
    ('AppName', 'CA Office Workforce Productivity Automation System', 'Application display name', 1);
GO

-- Seed System Users
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.tbl_Users (Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, Email, Phone, IsActive, IsDeleted, CreatedOn, CreatedBy)
    VALUES ('admin', 'zJ3H0S0g1X2k3L4m5N6o7P8q9R0s1T2u3V4w5X6y7Z8=', 'AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=', 'System Administrator', 3, 3, 'admin@caoffice.com', '9999999999', 1, 0, GETUTCDATE(), 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Users WHERE Username = 'owner')
BEGIN
    INSERT INTO dbo.tbl_Users (Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, Email, Phone, IsActive, IsDeleted, CreatedOn, CreatedBy)
    VALUES ('owner', 'zJ3H0S0g1X2k3L4m5N6o7P8q9R0s1T2u3V4w5X6y7Z8=', 'AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=', 'Firm Owner / Partner', 2, 3, 'owner@caoffice.com', '8888888888', 1, 0, GETUTCDATE(), 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Users WHERE Username = 'employee')
BEGIN
    INSERT INTO dbo.tbl_Users (Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, Email, Phone, IsActive, IsDeleted, CreatedOn, CreatedBy)
    VALUES ('employee', 'zJ3H0S0g1X2k3L4m5N6o7P8q9R0s1T2u3V4w5X6y7Z8=', 'AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=', 'Staff Member', 1, 1, 'staff@caoffice.com', '7777777777', 1, 0, GETUTCDATE(), 1);
END;
GO
