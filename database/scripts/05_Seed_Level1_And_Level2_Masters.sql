-- ===============================================================================
-- Script ID      : 05_Seed_Level1_And_Level2_Masters.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+
-- Description    : Seed Master Configurable Lookup Records for Level 1 and Level 2 Masters
-- ===============================================================================

USE [StaffAutomationDb];
GO

-- 1. Seed Roles
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Employee')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, CreatedBy) VALUES ('Employee', 'Staff member executing daily tasks and activity timeline', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Owner')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, CreatedBy) VALUES ('Owner', 'CA Firm Owner/Partner with executive dashboard & report access', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleName = 'Admin')
    INSERT INTO dbo.tbl_Roles (RoleName, Description, CreatedBy) VALUES ('Admin', 'System administrator maintaining master catalogs and audit logs', 1);
GO

-- 2. Seed Departments
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'ACCT')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, CreatedBy) VALUES ('ACCT', 'Accounting & Statutory Audit', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'TAX')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, CreatedBy) VALUES ('TAX', 'Income Tax & Indirect Tax (GST)', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE DepartmentCode = 'ADMIN')
    INSERT INTO dbo.tbl_Departments (DepartmentCode, DepartmentName, CreatedBy) VALUES ('ADMIN', 'Firm Administration & Secretarial', 1);
GO

-- 3. Seed Client Categories (Level 2 Master)
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ClientCategories WHERE CategoryCode = 'PVT_LTD')
    INSERT INTO dbo.tbl_ClientCategories (CategoryCode, CategoryName, Description, CreatedBy) VALUES 
    ('PVT_LTD', 'Private Limited Company', 'Private incorporated entity requiring MCA compliance & statutory audit', 1),
    ('PARTNERSHIP', 'Partnership Firm / LLP', 'Partnership entity or Limited Liability Partnership', 1),
    ('PROPRIETOR', 'Sole Proprietorship', 'Individual business unit', 1),
    ('INDIVIDUAL', 'Individual Taxpayer', 'Individual salaried or capital gain taxpayer', 1),
    ('TRUST_NGO', 'Trust / NGO', 'Section 8 or Charitable Trust', 1);
GO

-- 4. Seed Financial Years (Level 2 Master)
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_FinancialYears WHERE FYCode = 'FY2025-26')
    INSERT INTO dbo.tbl_FinancialYears (FYCode, StartDate, EndDate, IsCurrentFY, CreatedBy) VALUES 
    ('FY2024-25', '2024-04-01', '2025-03-31', 0, 1),
    ('FY2025-26', '2025-04-01', '2026-03-31', 0, 1),
    ('FY2026-27', '2026-04-01', '2027-03-31', 1, 1);
GO

-- 5. Seed Document Types (Level 2 Master)
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DocumentTypes WHERE TypeCode = 'BANK_STMT')
    INSERT INTO dbo.tbl_DocumentTypes (TypeCode, TypeName, DepartmentId, CreatedBy) VALUES 
    ('BANK_STMT', 'Bank Statement', 1, 1),
    ('SALES_REG', 'Sales Register / Invoices', 1, 1),
    ('PURCHASE_REG', 'Purchase Register / Invoices', 1, 1),
    ('FORM_16', 'Form 16 / 16A', 2, 1),
    ('FORM_26AS', 'Form 26AS / AIS / TIS', 2, 1),
    ('GST_2B', 'GSTR-2B Recon Sheet', 2, 1);
GO

-- 6. Seed Task Categories (Level 2 Master)
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskCategories WHERE CategoryCode = 'GSTR_3B')
    INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, DepartmentId, EstimatedHours, CreatedBy) VALUES 
    ('GSTR_3B', 'GSTR-3B Monthly Return Filing', 2, 1.50, 1),
    ('GSTR_1', 'GSTR-1 Sales Return Filing', 2, 2.00, 1),
    ('ITR_FILING', 'Income Tax Return Filing', 2, 3.00, 1),
    ('TDS_RETURN', 'TDS Quarterly Return Filing (26Q/27Q)', 2, 2.50, 1),
    ('BOOKKEEPING', 'Monthly Bookkeeping & Tally Entry', 1, 5.00, 1),
    ('TAX_AUDIT', 'Tax Audit Report (Form 3CD)', 2, 12.00, 1),
    ('STAT_AUDIT', 'Statutory Company Audit', 1, 20.00, 1);
GO

-- 7. Seed 11 Task Workflow States
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

-- 8. Seed Task Priorities
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskPriorities WHERE PriorityCode = 'LOW')
    INSERT INTO dbo.tbl_TaskPriorities (PriorityCode, PriorityName, ColorHexCode, SeverityRank, CreatedBy) VALUES 
    ('LOW', 'Low', '#28A745', 1, 1),
    ('MEDIUM', 'Medium', '#17A2B8', 2, 1),
    ('HIGH', 'High', '#FFC107', 3, 1),
    ('URGENT', 'Urgent', '#DC3545', 4, 1);
GO

-- 9. Seed Communication Types
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationTypes WHERE TypeCode = 'PHONE')
    INSERT INTO dbo.tbl_CommunicationTypes (TypeCode, TypeName, CreatedBy) VALUES 
    ('PHONE', 'Phone Call', 1),
    ('WHATSAPP', 'WhatsApp Message', 1),
    ('EMAIL', 'Email', 1),
    ('VISIT', 'Office Visit', 1),
    ('VIDEO', 'Video Call', 1);
GO

-- 10. Seed Communication Outcomes
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CommunicationOutcomes WHERE OutcomeCode = 'DOCS_REQ')
    INSERT INTO dbo.tbl_CommunicationOutcomes (OutcomeCode, OutcomeName, RequiresFollowUp, CreatedBy) VALUES 
    ('DOCS_REQ', 'Documents Requested', 1, 1),
    ('DOCS_REC', 'Documents Received', 0, 1),
    ('WAITING_REPLY', 'Waiting for Reply', 1, 1),
    ('EXPLAINED', 'Explained / Consulted', 0, 1),
    ('FOLLOWUP_REQ', 'Follow-up Required', 1, 1),
    ('RESOLVED', 'Completed / Resolved', 0, 1);
GO

-- 11. Seed Time Categories
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TimeCategories WHERE CategoryCode = 'WorkTime')
    INSERT INTO dbo.tbl_TimeCategories (CategoryCode, CategoryName, IsBillable, CreatedBy) VALUES 
    ('WorkTime', 'Work Execution Time', 1, 1),
    ('DiscussionTime', 'Client Discussion Time', 1, 1),
    ('WaitingTime', 'Waiting Time (Client/Docs)', 0, 1),
    ('BreakTime', 'Break / Non-Billable Time', 0, 1);
GO

-- 12. Seed Default System Settings
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SystemSettings WHERE SettingKey = 'AutoSaveIntervalSeconds')
    INSERT INTO dbo.tbl_SystemSettings (SettingKey, SettingValue, Description, ModifiedBy) VALUES 
    ('AutoSaveIntervalSeconds', '60', 'Interval in seconds for auto-saving active timer drafts', 1),
    ('MaxDashboardRefreshMs', '500', 'Performance ceiling for live owner dashboard refresh', 1),
    ('AppName', 'CA Office Workforce Productivity Automation System', 'Application display name', 1);
GO

-- 13. Seed Default System Users (Admin, Owner, Employee)
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

