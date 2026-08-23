-- ===============================================================================
-- Script ID      : 02_Level2_Business_Masters.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+
-- Description    : Level 2 Business Masters DDL (Users, Clients, Client Categories, Financial Years, Document Types, Task Categories, Task Templates)
-- ===============================================================================

USE [StaffAutomationDb];
GO

-- -------------------------------------------------------------------------------
-- 9. tbl_Users (System Accounts & Staff Master)
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

-- -------------------------------------------------------------------------------
-- 10. tbl_ClientCategories (Client Entity Legal Structure / Classification)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 11. tbl_Clients (Client Master Registry)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 12. tbl_FinancialYears (Financial Year Master e.g. FY 2025-26, FY 2026-27)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 13. tbl_DocumentTypes (Voucher & Tax Supporting Document Types Master)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 14. tbl_TaskCategories (Service Catalog & Task Category Master)
-- -------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------
-- 15. tbl_TaskTemplates (Standardized Compliance Task Checklist Master)
-- -------------------------------------------------------------------------------
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

-- Compatibility Views for Singular Table Names
IF OBJECT_ID(N'dbo.tbl_User', N'V') IS NOT NULL DROP VIEW dbo.tbl_User;
GO
CREATE VIEW dbo.tbl_User AS SELECT * FROM dbo.tbl_Users;
GO

IF OBJECT_ID(N'dbo.tbl_Client', N'V') IS NOT NULL DROP VIEW dbo.tbl_Client;
GO
CREATE VIEW dbo.tbl_Client AS SELECT * FROM dbo.tbl_Clients;
GO

