-- ===============================================================================
-- Script ID      : 03_Create_Operational_Tables.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+
-- Description    : DDL for Level 3 Transactional Tables (Tasks, Activities, Discussions, Attendance, Notifications, Audit Logs)
-- ===============================================================================

USE [StaffAutomationDb];
GO

-- -------------------------------------------------------------------------------
-- 16. tbl_Tasks (Central Operational Task Entity)
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

-- -------------------------------------------------------------------------------
-- 17. tbl_TaskActivities (Chronological Task Execution Entries)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 18. tbl_Discussions (Structured Client Communication Logs)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 19. tbl_Attendance (Employee Clock-In/Out & Break Tracking)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 20. tbl_Notifications (Future-Ready System Notification Queue)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 21. tbl_AuditLogs (System Security & Audit Logging)
-- -------------------------------------------------------------------------------
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

-- Compatibility Views for Singular Table Names
IF OBJECT_ID(N'dbo.tbl_Task', N'V') IS NOT NULL DROP VIEW dbo.tbl_Task;
GO
CREATE VIEW dbo.tbl_Task AS SELECT * FROM dbo.tbl_Tasks;
GO

IF OBJECT_ID(N'dbo.tbl_Discussion', N'V') IS NOT NULL DROP VIEW dbo.tbl_Discussion;
GO
CREATE VIEW dbo.tbl_Discussion AS SELECT * FROM dbo.tbl_Discussions;
GO

IF OBJECT_ID(N'dbo.tbl_TaskActivity', N'V') IS NOT NULL DROP VIEW dbo.tbl_TaskActivity;
GO
CREATE VIEW dbo.tbl_TaskActivity AS SELECT * FROM dbo.tbl_TaskActivities;
GO
