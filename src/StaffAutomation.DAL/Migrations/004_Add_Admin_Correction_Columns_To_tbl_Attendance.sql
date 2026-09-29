-- ========================================================================================
-- Script: 08_Add_Admin_Correction_Columns_To_tbl_Attendance.sql
-- Description: Adds administrative correction tracking & audit columns to dbo.tbl_Attendance.
-- Authoritative, non-destructive schema migration.
-- ========================================================================================

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbl_Attendance')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_Attendance') AND name = N'IsManuallyCorrected')
    BEGIN
        ALTER TABLE dbo.tbl_Attendance ADD IsManuallyCorrected BIT NOT NULL CONSTRAINT DF_tbl_Attendance_IsCorrected DEFAULT (0);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_Attendance') AND name = N'CorrectionReason')
    BEGIN
        ALTER TABLE dbo.tbl_Attendance ADD CorrectionReason NVARCHAR(255) NULL;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_Attendance') AND name = N'CorrectedBy')
    BEGIN
        ALTER TABLE dbo.tbl_Attendance ADD CorrectedBy INT NULL CONSTRAINT FK_tbl_Attendance_CorrectedBy REFERENCES dbo.tbl_Users(UserId);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_Attendance') AND name = N'CorrectedOn')
    BEGIN
        ALTER TABLE dbo.tbl_Attendance ADD CorrectedOn DATETIME2 NULL;
    END;
END;
GO
