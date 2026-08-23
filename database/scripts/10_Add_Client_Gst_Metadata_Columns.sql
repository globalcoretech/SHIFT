-- ============================================================================
-- Script: 10_Add_Client_Gst_Metadata_Columns.sql
-- Description: Idempotently adds GST API and address metadata columns to dbo.tbl_Clients
-- Author: StaffAutomation Engineering Team
-- Date: 2026-08-22
-- ============================================================================

USE [StaffAutomationDb];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Clients') AND name = 'IsLiveApiData')
BEGIN
    ALTER TABLE dbo.tbl_Clients ADD 
        StateName NVARCHAR(100) NULL,
        District NVARCHAR(100) NULL,
        Pincode NVARCHAR(20) NULL,
        Address NVARCHAR(MAX) NULL,
        EntityType NVARCHAR(100) NULL,
        GstType NVARCHAR(100) NULL,
        IsLiveApiData BIT NOT NULL CONSTRAINT DF_tbl_Clients_IsLiveApiData DEFAULT (0);
    PRINT 'SUCCESS: GST metadata columns added to dbo.tbl_Clients.';
END
ELSE
BEGIN
    PRINT 'INFO: GST metadata columns already exist in dbo.tbl_Clients.';
END
GO
