-- ===============================================================================
-- Script ID      : 07_Add_BreakStartTime_To_tbl_Attendance.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+
-- Description    : Idempotent migration script adding BreakStartTime column to dbo.tbl_Attendance
-- ===============================================================================

USE [StaffAutomationDb];
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbl_Attendance')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_Attendance') AND name = N'BreakStartTime')
    BEGIN
        ALTER TABLE dbo.tbl_Attendance ADD BreakStartTime DATETIME2 NULL;
        PRINT 'Column BreakStartTime added successfully to dbo.tbl_Attendance.';
    END
    ELSE
    BEGIN
        PRINT 'Column BreakStartTime already exists in dbo.tbl_Attendance.';
    END
END;
GO
