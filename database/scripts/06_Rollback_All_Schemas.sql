-- ===============================================================================
-- Script ID      : 06_Rollback_All_Schemas.sql
-- Project        : CA Office Workforce Productivity Automation System
-- Target Engine  : SQL Server Express 2016+ / SQL Server 2019+ / SQL Server 2022+
-- Description    : SQL Rollback Script to safely tear down all tables, views, and indexes in reverse dependency order
-- WARNING        : THIS SCRIPT DROPS ALL TABLES AND DATA IN StaffAutomationDb!
-- ===============================================================================

USE [StaffAutomationDb];
GO

-- 1. Drop Views (Dashboard and Compatibility Views)
IF OBJECT_ID(N'dbo.vw_Client360History', N'V') IS NOT NULL DROP VIEW dbo.vw_Client360History;
IF OBJECT_ID(N'dbo.vw_MonthlyEmployeeProductivity', N'V') IS NOT NULL DROP VIEW dbo.vw_MonthlyEmployeeProductivity;
IF OBJECT_ID(N'dbo.vw_LiveOwnerDashboard', N'V') IS NOT NULL DROP VIEW dbo.vw_LiveOwnerDashboard;

IF OBJECT_ID(N'dbo.tbl_TaskActivity', N'V') IS NOT NULL DROP VIEW dbo.tbl_TaskActivity;
IF OBJECT_ID(N'dbo.tbl_Discussion', N'V') IS NOT NULL DROP VIEW dbo.tbl_Discussion;
IF OBJECT_ID(N'dbo.tbl_Task', N'V') IS NOT NULL DROP VIEW dbo.tbl_Task;
IF OBJECT_ID(N'dbo.tbl_Client', N'V') IS NOT NULL DROP VIEW dbo.tbl_Client;
IF OBJECT_ID(N'dbo.tbl_User', N'V') IS NOT NULL DROP VIEW dbo.tbl_User;
GO

-- 2. Drop Level 3 Operational Tables (Reverse Dependency Order)
IF OBJECT_ID(N'dbo.tbl_AuditLogs', N'U') IS NOT NULL DROP TABLE dbo.tbl_AuditLogs;
IF OBJECT_ID(N'dbo.tbl_Notifications', N'U') IS NOT NULL DROP TABLE dbo.tbl_Notifications;
IF OBJECT_ID(N'dbo.tbl_Attendance', N'U') IS NOT NULL DROP TABLE dbo.tbl_Attendance;
IF OBJECT_ID(N'dbo.tbl_Discussions', N'U') IS NOT NULL DROP TABLE dbo.tbl_Discussions;
IF OBJECT_ID(N'dbo.tbl_TaskActivities', N'U') IS NOT NULL DROP TABLE dbo.tbl_TaskActivities;
IF OBJECT_ID(N'dbo.tbl_Tasks', N'U') IS NOT NULL DROP TABLE dbo.tbl_Tasks;
GO

-- 3. Drop Level 2 Business Masters
IF OBJECT_ID(N'dbo.tbl_TaskTemplates', N'U') IS NOT NULL DROP TABLE dbo.tbl_TaskTemplates;
IF OBJECT_ID(N'dbo.tbl_TaskCategories', N'U') IS NOT NULL DROP TABLE dbo.tbl_TaskCategories;
IF OBJECT_ID(N'dbo.tbl_DocumentTypes', N'U') IS NOT NULL DROP TABLE dbo.tbl_DocumentTypes;
IF OBJECT_ID(N'dbo.tbl_FinancialYears', N'U') IS NOT NULL DROP TABLE dbo.tbl_FinancialYears;
IF OBJECT_ID(N'dbo.tbl_Clients', N'U') IS NOT NULL DROP TABLE dbo.tbl_Clients;
IF OBJECT_ID(N'dbo.tbl_ClientCategories', N'U') IS NOT NULL DROP TABLE dbo.tbl_ClientCategories;
IF OBJECT_ID(N'dbo.tbl_Users', N'U') IS NOT NULL DROP TABLE dbo.tbl_Users;
GO

-- 4. Drop Level 1 System Masters
IF OBJECT_ID(N'dbo.tbl_SystemSettings', N'U') IS NOT NULL DROP TABLE dbo.tbl_SystemSettings;
IF OBJECT_ID(N'dbo.tbl_TimeCategories', N'U') IS NOT NULL DROP TABLE dbo.tbl_TimeCategories;
IF OBJECT_ID(N'dbo.tbl_CommunicationOutcomes', N'U') IS NOT NULL DROP TABLE dbo.tbl_CommunicationOutcomes;
IF OBJECT_ID(N'dbo.tbl_CommunicationTypes', N'U') IS NOT NULL DROP TABLE dbo.tbl_CommunicationTypes;
IF OBJECT_ID(N'dbo.tbl_TaskPriorities', N'U') IS NOT NULL DROP TABLE dbo.tbl_TaskPriorities;
IF OBJECT_ID(N'dbo.tbl_TaskStatuses', N'U') IS NOT NULL DROP TABLE dbo.tbl_TaskStatuses;
IF OBJECT_ID(N'dbo.tbl_Departments', N'U') IS NOT NULL DROP TABLE dbo.tbl_Departments;
IF OBJECT_ID(N'dbo.tbl_Roles', N'U') IS NOT NULL DROP TABLE dbo.tbl_Roles;
GO
