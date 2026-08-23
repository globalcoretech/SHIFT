-- ===============================================================================
-- Script ID      : 04_Create_Indexes_And_Views.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+ / SQL Server 2022+
-- Description    : DDL for Non-Clustered Indexes, Compatibility Views, and Dashboard Views
-- ===============================================================================

USE [StaffAutomationDb];
GO

-- -------------------------------------------------------------------------------
-- Session Settings: Complete Set Required for Filtered Indexes & Complex Views
-- -------------------------------------------------------------------------------
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- -------------------------------------------------------------------------------
-- Indexing Strategy: Optimized for Dashboard (< 320ms) and Excel Reports (< 1.0s)
-- -------------------------------------------------------------------------------

-- Index 1: Fast filtering of active tasks by assigned staff and status
IF OBJECT_ID(N'dbo.tbl_Tasks', N'U') IS NOT NULL 
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_Tasks_AssignedTo_Status' AND object_id = OBJECT_ID(N'dbo.tbl_Tasks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_Tasks_AssignedTo_Status
    ON dbo.tbl_Tasks (AssignedToUserId, StatusId)
    INCLUDE (TaskCode, Title, ClientId, TargetDueDate, PriorityId)
    WHERE IsDeleted = 0;
END;
GO

-- Index 2: Instant lookup of client-wise task history
IF OBJECT_ID(N'dbo.tbl_Tasks', N'U') IS NOT NULL 
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_Tasks_ClientId_DueDate' AND object_id = OBJECT_ID(N'dbo.tbl_Tasks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_Tasks_ClientId_DueDate
    ON dbo.tbl_Tasks (ClientId, TargetDueDate DESC)
    INCLUDE (TaskCode, Title, AssignedToUserId, StatusId)
    WHERE IsDeleted = 0;
END;
GO

-- Index 3: Rapid daily timeline extraction for staff
IF OBJECT_ID(N'dbo.tbl_TaskActivities', N'U') IS NOT NULL 
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_TaskActivities_UserId_ActivityDate' AND object_id = OBJECT_ID(N'dbo.tbl_TaskActivities'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_TaskActivities_UserId_ActivityDate
    ON dbo.tbl_TaskActivities (UserId, ActivityDate DESC)
    INCLUDE (TaskId, TimeCategoryId, DurationMinutes, StartTime, EndTime);
END;
GO

-- Index 3B: Database-Level Idempotency Protection for Checklist Step Completions
IF OBJECT_ID(N'dbo.tbl_TaskActivities', N'U') IS NOT NULL 
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_TaskActivities_TaskId_ChecklistStep' AND object_id = OBJECT_ID(N'dbo.tbl_TaskActivities'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_tbl_TaskActivities_TaskId_ChecklistStep
    ON dbo.tbl_TaskActivities (TaskId, ActivityDescription)
    WHERE ActivityDescription LIKE '[CHECKLIST_COMPLETE]%';
END;
GO

-- Index 4: Fast client discussion timeline lookup
IF OBJECT_ID(N'dbo.tbl_Discussions', N'U') IS NOT NULL 
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_Discussions_ClientId_Timestamp' AND object_id = OBJECT_ID(N'dbo.tbl_Discussions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_Discussions_ClientId_Timestamp
    ON dbo.tbl_Discussions (ClientId, DiscussionTimestamp DESC)
    INCLUDE (TaskId, UserId, CommunicationTypeId, OutcomeId, DurationMinutes);
END;
GO

-- Index 5: Attendance date reconciliation lookup
IF OBJECT_ID(N'dbo.tbl_Attendance', N'U') IS NOT NULL 
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_Attendance_Date' AND object_id = OBJECT_ID(N'dbo.tbl_Attendance'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_Attendance_Date
    ON dbo.tbl_Attendance (AttendanceDate DESC, UserId)
    INCLUDE (ClockInTime, ClockOutTime, TotalWorkingMinutes);
END;
GO

-- -------------------------------------------------------------------------------
-- View 1: vw_LiveOwnerDashboard (Pre-aggregated Dashboard View for 12 KPIs)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.vw_LiveOwnerDashboard') IS NOT NULL
    DROP VIEW dbo.vw_LiveOwnerDashboard;
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

-- -------------------------------------------------------------------------------
-- View 2: vw_MonthlyEmployeeProductivity (Pre-aggregated View for Excel Export)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.vw_MonthlyEmployeeProductivity') IS NOT NULL
    DROP VIEW dbo.vw_MonthlyEmployeeProductivity;
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

-- -------------------------------------------------------------------------------
-- View 3: vw_Client360History (Client-Wise 360 Degree Work & Communication History)
-- -------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.vw_Client360History') IS NOT NULL
    DROP VIEW dbo.vw_Client360History;
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

-- -------------------------------------------------------------------------------
-- Compatibility Views for Singular Table Alias Support
-- -------------------------------------------------------------------------------
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
