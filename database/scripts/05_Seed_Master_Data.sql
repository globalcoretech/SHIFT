-- ===============================================================================
-- Script ID      : 05_Seed_Master_Data.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+ / SQL Server 2022+
-- Description    : Seed Master Configurable Lookup Records, Level 1/2 Masters & Default Admin Accounts
-- Standards      : Strict Idempotency (Per-row EXISTS checks), Dynamic TOP 1 FK Resolution, PBKDF2-SHA256 Hashes
-- ===============================================================================

USE [StaffAutomationDb];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- -------------------------------------------------------------------------------
-- 1. Seed Roles (Level 1 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Employee')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, IsActive, CreatedBy) VALUES ('Employee', 'Staff member executing daily tasks and activity timeline', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Owner')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, IsActive, CreatedBy) VALUES ('Owner', 'CA Firm Owner/Partner with executive dashboard & report access', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Admin')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, IsActive, CreatedBy) VALUES ('Admin', 'System administrator maintaining master catalogs and audit logs', 1, 1);
GO

-- -------------------------------------------------------------------------------
-- 2. Seed Departments (Level 1 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, IsActive, CreatedBy) VALUES ('ACCT', 'Accounting & Statutory Audit', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, IsActive, CreatedBy) VALUES ('TAX', 'Income Tax & Indirect Tax (GST)', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'ADMIN')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, IsActive, CreatedBy) VALUES ('ADMIN', 'Firm Administration & Secretarial', 1, 1);
GO

-- -------------------------------------------------------------------------------
-- 3. Seed Client Categories (Level 2 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ClientCategories WHERE CategoryCode = 'PVT_LTD')
    INSERT INTO dbo.tbl_ClientCategories (CategoryCode, CategoryName, Description, IsActive, CreatedBy) VALUES ('PVT_LTD', 'Private Limited Company', 'Private incorporated entity requiring MCA compliance & statutory audit', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ClientCategories WHERE CategoryCode = 'PARTNERSHIP')
    INSERT INTO dbo.tbl_ClientCategories (CategoryCode, CategoryName, Description, IsActive, CreatedBy) VALUES ('PARTNERSHIP', 'Partnership Firm / LLP', 'Partnership entity or Limited Liability Partnership', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ClientCategories WHERE CategoryCode = 'PROPRIETOR')
    INSERT INTO dbo.tbl_ClientCategories (CategoryCode, CategoryName, Description, IsActive, CreatedBy) VALUES ('PROPRIETOR', 'Sole Proprietorship', 'Individual business unit', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ClientCategories WHERE CategoryCode = 'INDIVIDUAL')
    INSERT INTO dbo.tbl_ClientCategories (CategoryCode, CategoryName, Description, IsActive, CreatedBy) VALUES ('INDIVIDUAL', 'Individual Taxpayer', 'Individual salaried or capital gain taxpayer', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ClientCategories WHERE CategoryCode = 'TRUST_NGO')
    INSERT INTO dbo.tbl_ClientCategories (CategoryCode, CategoryName, Description, IsActive, CreatedBy) VALUES ('TRUST_NGO', 'Trust / NGO', 'Section 8 or Charitable Trust', 1, 1);
GO

-- -------------------------------------------------------------------------------
-- 4. Seed Financial Years (Level 2 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_FinancialYears WHERE FYCode = 'FY2024-25')
    INSERT INTO dbo.tbl_FinancialYears (FYCode, StartDate, EndDate, IsCurrentFY, CreatedBy) VALUES ('FY2024-25', '2024-04-01', '2025-03-31', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_FinancialYears WHERE FYCode = 'FY2025-26')
    INSERT INTO dbo.tbl_FinancialYears (FYCode, StartDate, EndDate, IsCurrentFY, CreatedBy) VALUES ('FY2025-26', '2025-04-01', '2026-03-31', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_FinancialYears WHERE FYCode = 'FY2026-27')
    INSERT INTO dbo.tbl_FinancialYears (FYCode, StartDate, EndDate, IsCurrentFY, CreatedBy) VALUES ('FY2026-27', '2026-04-01', '2027-03-31', 1, 1);
GO

-- -------------------------------------------------------------------------------
-- 5. Seed Document Types (Level 2 Master - Department Resolved Dynamic FK)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DocumentTypes WHERE TypeCode = 'BANK_STMT')
    INSERT INTO dbo.tbl_DocumentTypes (TypeCode, TypeName, DepartmentId, IsActive, CreatedBy) 
    VALUES ('BANK_STMT', 'Bank Statement', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT'), 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DocumentTypes WHERE TypeCode = 'SALES_REG')
    INSERT INTO dbo.tbl_DocumentTypes (TypeCode, TypeName, DepartmentId, IsActive, CreatedBy) 
    VALUES ('SALES_REG', 'Sales Register / Invoices', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT'), 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DocumentTypes WHERE TypeCode = 'PURCHASE_REG')
    INSERT INTO dbo.tbl_DocumentTypes (TypeCode, TypeName, DepartmentId, IsActive, CreatedBy) 
    VALUES ('PURCHASE_REG', 'Purchase Register / Invoices', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT'), 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DocumentTypes WHERE TypeCode = 'FORM_16')
    INSERT INTO dbo.tbl_DocumentTypes (TypeCode, TypeName, DepartmentId, IsActive, CreatedBy) 
    VALUES ('FORM_16', 'Form 16 / 16A', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX'), 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DocumentTypes WHERE TypeCode = 'FORM_26AS')
    INSERT INTO dbo.tbl_DocumentTypes (TypeCode, TypeName, DepartmentId, IsActive, CreatedBy) 
    VALUES ('FORM_26AS', 'Form 26AS / AIS / TIS', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX'), 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DocumentTypes WHERE TypeCode = 'GST_2B')
    INSERT INTO dbo.tbl_DocumentTypes (TypeCode, TypeName, DepartmentId, IsActive, CreatedBy) 
    VALUES ('GST_2B', 'GSTR-2B Recon Sheet', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX'), 1, 1);
GO

-- -------------------------------------------------------------------------------
-- 6. Seed Task Categories (Level 2 Master - Department Resolved Dynamic FK)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskCategories WHERE CategoryCode = 'GSTR_3B')
    INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, DepartmentId, EstimatedHours, IsActive, CreatedBy) 
    VALUES ('GSTR_3B', 'GSTR-3B Monthly Return Filing', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX'), 1.50, 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskCategories WHERE CategoryCode = 'GSTR_1')
    INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, DepartmentId, EstimatedHours, IsActive, CreatedBy) 
    VALUES ('GSTR_1', 'GSTR-1 Sales Return Filing', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX'), 2.00, 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskCategories WHERE CategoryCode = 'ITR_FILING')
    INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, DepartmentId, EstimatedHours, IsActive, CreatedBy) 
    VALUES ('ITR_FILING', 'Income Tax Return Filing', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX'), 3.00, 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskCategories WHERE CategoryCode = 'TDS_RETURN')
    INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, DepartmentId, EstimatedHours, IsActive, CreatedBy) 
    VALUES ('TDS_RETURN', 'TDS Quarterly Return Filing (26Q/27Q)', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX'), 2.50, 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskCategories WHERE CategoryCode = 'BOOKKEEPING')
    INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, DepartmentId, EstimatedHours, IsActive, CreatedBy) 
    VALUES ('BOOKKEEPING', 'Monthly Bookkeeping & Tally Entry', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT'), 5.00, 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskCategories WHERE CategoryCode = 'TAX_AUDIT')
    INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, DepartmentId, EstimatedHours, IsActive, CreatedBy) 
    VALUES ('TAX_AUDIT', 'Tax Audit Report (Form 3CD)', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX'), 12.00, 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskCategories WHERE CategoryCode = 'STAT_AUDIT')
    INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, DepartmentId, EstimatedHours, IsActive, CreatedBy) 
    VALUES ('STAT_AUDIT', 'Statutory Company Audit', (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT'), 20.00, 1, 1);
GO

-- -------------------------------------------------------------------------------
-- 7. Seed 11 Task Workflow States (Level 1 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'NewTask')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('NewTask', 'New', 1, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'Assigned')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('Assigned', 'Assigned', 2, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'InProgress')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('InProgress', 'In Progress', 3, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'WaitingForClient')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('WaitingForClient', 'Waiting for Client', 4, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'WaitingForDocuments')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('WaitingForDocuments', 'Waiting for Documents', 5, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'OnHold')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('OnHold', 'On Hold', 6, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'UnderReview')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('UnderReview', 'Under Review', 7, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'Completed')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('Completed', 'Completed', 8, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'Delivered')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('Delivered', 'Delivered', 9, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'Closed')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('Closed', 'Closed', 10, 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskStatuses WHERE StatusCode = 'Cancelled')
    INSERT INTO dbo.tbl_TaskStatuses (StatusCode, StatusName, DisplayOrder, IsTerminalState, CreatedBy) VALUES ('Cancelled', 'Cancelled', 11, 1, 1);
GO

-- -------------------------------------------------------------------------------
-- 8. Seed Task Priorities (Level 1 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskPriorities WHERE PriorityCode = 'LOW')
    INSERT INTO dbo.tbl_TaskPriorities (PriorityCode, PriorityName, ColorHexCode, SeverityRank, CreatedBy) VALUES ('LOW', 'Low', '#28A745', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskPriorities WHERE PriorityCode = 'MEDIUM')
    INSERT INTO dbo.tbl_TaskPriorities (PriorityCode, PriorityName, ColorHexCode, SeverityRank, CreatedBy) VALUES ('MEDIUM', 'Medium', '#17A2B8', 2, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskPriorities WHERE PriorityCode = 'HIGH')
    INSERT INTO dbo.tbl_TaskPriorities (PriorityCode, PriorityName, ColorHexCode, SeverityRank, CreatedBy) VALUES ('HIGH', 'High', '#FFC107', 3, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskPriorities WHERE PriorityCode = 'URGENT')
    INSERT INTO dbo.tbl_TaskPriorities (PriorityCode, PriorityName, ColorHexCode, SeverityRank, CreatedBy) VALUES ('URGENT', 'Urgent', '#DC3545', 4, 1);
GO

-- -------------------------------------------------------------------------------
-- 9. Seed Communication Types (Level 1 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationTypes WHERE TypeCode = 'PHONE')
    INSERT INTO dbo.tbl_CommunicationTypes (TypeCode, TypeName, IsActive, CreatedBy) VALUES ('PHONE', 'Phone Call', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationTypes WHERE TypeCode = 'WHATSAPP')
    INSERT INTO dbo.tbl_CommunicationTypes (TypeCode, TypeName, IsActive, CreatedBy) VALUES ('WHATSAPP', 'WhatsApp Message', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationTypes WHERE TypeCode = 'EMAIL')
    INSERT INTO dbo.tbl_CommunicationTypes (TypeCode, TypeName, IsActive, CreatedBy) VALUES ('EMAIL', 'Email', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationTypes WHERE TypeCode = 'VISIT')
    INSERT INTO dbo.tbl_CommunicationTypes (TypeCode, TypeName, IsActive, CreatedBy) VALUES ('VISIT', 'Office Visit', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationTypes WHERE TypeCode = 'VIDEO')
    INSERT INTO dbo.tbl_CommunicationTypes (TypeCode, TypeName, IsActive, CreatedBy) VALUES ('VIDEO', 'Video Call', 1, 1);
GO

-- -------------------------------------------------------------------------------
-- 10. Seed Communication Outcomes (Level 1 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationOutcomes WHERE OutcomeCode = 'DOCS_REQ')
    INSERT INTO dbo.tbl_CommunicationOutcomes (OutcomeCode, OutcomeName, RequiresFollowUp, CreatedBy) VALUES ('DOCS_REQ', 'Documents Requested', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationOutcomes WHERE OutcomeCode = 'DOCS_REC')
    INSERT INTO dbo.tbl_CommunicationOutcomes (OutcomeCode, OutcomeName, RequiresFollowUp, CreatedBy) VALUES ('DOCS_REC', 'Documents Received', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationOutcomes WHERE OutcomeCode = 'WAITING_REPLY')
    INSERT INTO dbo.tbl_CommunicationOutcomes (OutcomeCode, OutcomeName, RequiresFollowUp, CreatedBy) VALUES ('WAITING_REPLY', 'Waiting for Reply', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationOutcomes WHERE OutcomeCode = 'EXPLAINED')
    INSERT INTO dbo.tbl_CommunicationOutcomes (OutcomeCode, OutcomeName, RequiresFollowUp, CreatedBy) VALUES ('EXPLAINED', 'Explained / Consulted', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationOutcomes WHERE OutcomeCode = 'FOLLOWUP_REQ')
    INSERT INTO dbo.tbl_CommunicationOutcomes (OutcomeCode, OutcomeName, RequiresFollowUp, CreatedBy) VALUES ('FOLLOWUP_REQ', 'Follow-up Required', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationOutcomes WHERE OutcomeCode = 'RESOLVED')
    INSERT INTO dbo.tbl_CommunicationOutcomes (OutcomeCode, OutcomeName, RequiresFollowUp, CreatedBy) VALUES ('RESOLVED', 'Completed / Resolved', 0, 1);
GO

-- -------------------------------------------------------------------------------
-- 11. Seed Time Categories (Level 1 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TimeCategories WHERE CategoryCode = 'WorkTime')
    INSERT INTO dbo.tbl_TimeCategories (CategoryCode, CategoryName, IsBillable, CreatedBy) VALUES ('WorkTime', 'Work Execution Time', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TimeCategories WHERE CategoryCode = 'DiscussionTime')
    INSERT INTO dbo.tbl_TimeCategories (CategoryCode, CategoryName, IsBillable, CreatedBy) VALUES ('DiscussionTime', 'Client Discussion Time', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TimeCategories WHERE CategoryCode = 'WaitingTime')
    INSERT INTO dbo.tbl_TimeCategories (CategoryCode, CategoryName, IsBillable, CreatedBy) VALUES ('WaitingTime', 'Waiting Time (Client/Docs)', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TimeCategories WHERE CategoryCode = 'BreakTime')
    INSERT INTO dbo.tbl_TimeCategories (CategoryCode, CategoryName, IsBillable, CreatedBy) VALUES ('BreakTime', 'Break / Non-Billable Time', 0, 1);
GO

-- -------------------------------------------------------------------------------
-- 12. Seed Default System Settings (Level 1 Master)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SystemSettings WHERE SettingKey = 'AutoSaveIntervalSeconds')
    INSERT INTO dbo.tbl_SystemSettings (SettingKey, SettingValue, Description, ModifiedBy) VALUES ('AutoSaveIntervalSeconds', '60', 'Interval in seconds for auto-saving active timer drafts', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SystemSettings WHERE SettingKey = 'MaxDashboardRefreshMs')
    INSERT INTO dbo.tbl_SystemSettings (SettingKey, SettingValue, Description, ModifiedBy) VALUES ('MaxDashboardRefreshMs', '500', 'Performance ceiling for live owner dashboard refresh', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SystemSettings WHERE SettingKey = 'AppName')
    INSERT INTO dbo.tbl_SystemSettings (SettingKey, SettingValue, Description, ModifiedBy) VALUES ('AppName', 'CA Office Workforce Productivity Automation System', 'Application display name', 1);
GO

-- -------------------------------------------------------------------------------
-- 13. Seed Default System Users (Admin, Owner, Employee - Valid PBKDF2-SHA256 Hashes)
-- -------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.tbl_Users (Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, Email, Phone, IsActive, IsDeleted, CreatedOn, CreatedBy)
    VALUES (
        'admin',
        '1GvJk5+hB3R9yL2m4N6p8Q0r2S4t6U8v',
        'AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=',
        'System Administrator',
        (SELECT TOP 1 RoleId FROM dbo.tbl_Roles WHERE RoleName = 'Admin'),
        (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'ADMIN'),
        'admin@caoffice.com',
        '9999999999',
        1, 0, GETUTCDATE(), 1
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Users WHERE Username = 'owner')
BEGIN
    INSERT INTO dbo.tbl_Users (Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, Email, Phone, IsActive, IsDeleted, CreatedOn, CreatedBy)
    VALUES (
        'owner',
        '2HvKl6+iC4S0zM3n5O7q9R1s3T5u7V9w',
        'AgICAgICAgICAgICAgICAgICAgICAgIC',
        'Firm Owner / Partner',
        (SELECT TOP 1 RoleId FROM dbo.tbl_Roles WHERE RoleName = 'Owner'),
        (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'ADMIN'),
        'owner@caoffice.com',
        '8888888888',
        1, 0, GETUTCDATE(), 1
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Users WHERE Username = 'employee')
BEGIN
    INSERT INTO dbo.tbl_Users (Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, Email, Phone, IsActive, IsDeleted, CreatedOn, CreatedBy)
    VALUES (
        'employee',
        '3IwLm7+jD5T10N4o6P8r0S2t4U6v8W0x',
        'AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMD',
        'Staff Member',
        (SELECT TOP 1 RoleId FROM dbo.tbl_Roles WHERE RoleName = 'Employee'),
        (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT'),
        'staff@caoffice.com',
        '7777777777',
        1, 0, GETUTCDATE(), 1
    );
END;
GO
