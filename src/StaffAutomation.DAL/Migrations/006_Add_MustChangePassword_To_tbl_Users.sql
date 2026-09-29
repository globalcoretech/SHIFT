-- ============================================================================
-- Script: 09_Add_MustChangePassword_To_tbl_Users.sql
-- Description: Idempotently adds MustChangePassword column to dbo.tbl_Users
-- Author: StaffAutomation Engineering Team
-- Date: 2026-08-22
-- ============================================================================

USE [StaffAutomationDb];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Users') AND name = 'MustChangePassword')
BEGIN
    ALTER TABLE dbo.tbl_Users ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_tbl_Users_MustChangePassword DEFAULT (0);
    PRINT 'SUCCESS: MustChangePassword column added to dbo.tbl_Users.';
END
ELSE
BEGIN
    PRINT 'INFO: MustChangePassword column already exists in dbo.tbl_Users.';
END
GO
