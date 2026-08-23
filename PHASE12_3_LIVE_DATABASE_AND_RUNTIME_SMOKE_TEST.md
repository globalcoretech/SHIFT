# Phase 12.3 — Live Database Migration & Runtime Smoke Test Report

## Project
**CA Office Workforce Productivity Automation System**

## Implementation Phase
**Phase 12.3 — Live Database Migration & Runtime Smoke Test**

---

## 1. Executive Summary & Verification Classification

In accordance with strict verification directives, all verification items in this report are categorized into **ACTUALLY EXECUTED AND PASSED**, **ACTUALLY EXECUTED AND FAILED**, or **UNVERIFIED / NOT EXECUTED**.

### Final Status Designation: `PARTIAL PASS`
- **Reason for Partial Status**: The static configuration audit, DDL script readiness, and code architecture were verified. However, live SQL Server database execution, backup creation, `dotnet build` / `dotnet test` execution, and WinForms application runtime smoke tests could not be executed because the local automated terminal runner was blocked by environment host security restrictions (`Access is denied.`).

---

## 2. Verification Categorization Matrix

### Category A: ACTUALLY EXECUTED AND PASSED
1. **Pre-flight Configuration Inspection**:
   - **ConfigFile Inspected**: `src/StaffAutomation.WinForms/App.config`
   - **Detected Connection String**: `Server=.\SQLEXPRESS;Database=StaffAutomationDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connect Timeout=15;Pooling=True;Max Pool Size=50;`
   - **Detected SQL Server Instance**: `.\SQLEXPRESS` (Local SQL Server Express)
   - **Detected Target Database**: `StaffAutomationDb`
   - **Configuration Integrity**: Preserved without modifications.

2. **DDL Migration Script Readiness & Syntax Audit**:
   - **Script Inspected**: `database/migrations/11_Phase11_Core_Configuration_And_Security_Tables.sql`
   - **DDL Structure**: Validated to contain non-destructive, 100% idempotent table creation (`IF OBJECT_ID IS NULL`), column extensions (`IF NOT EXISTS`), index creations (`IF NOT EXISTS`), permission catalog seeding (`MERGE`), and dynamic role mappings (`WHERE NOT EXISTS`).

---

### Category B: ACTUALLY EXECUTED AND FAILED
- *None.* No execution failed due to script or code errors.

---

### Category C: UNVERIFIED / NOT EXECUTED
*Due to terminal runner process execution restriction (`CORTEX_STEP_TYPE_RUN_COMMAND: revoking inherited access: Access is denied.`)*

1. **Live SQL Server Instance Connection**: UNVERIFIED (Terminal access revoked)
2. **Pre-Migration Safety Backup Execution**: UNVERIFIED (Terminal access revoked)
3. **Phase 11 Migration Script Execution**: UNVERIFIED (Terminal access revoked)
4. **Post-Migration Database Schema & Seed Verification Queries**: UNVERIFIED (Terminal access revoked)
5. **Migration Idempotency Second-Run Execution**: UNVERIFIED (Terminal access revoked)
6. **Existing Database Record Integrity Queries**: UNVERIFIED (Terminal access revoked)
7. **CLI Solution Build (`dotnet build`) Execution**: UNVERIFIED (Terminal access revoked)
8. **CLI Test Runner (`dotnet test`) Execution**: UNVERIFIED (Terminal access revoked)
9. **WinForms Application GUI Launch & Smoke Test**: UNVERIFIED (GUI launch restricted)
10. **Runtime Granular Authorization Smoke Test**: UNVERIFIED (GUI launch restricted)

---

## 3. Detected Application Database Configuration

| Configuration Property | Value Detected from `App.config` |
| :--- | :--- |
| **Connection String Name** | `StaffAutomationDb` |
| **SQL Server Instance** | `.\SQLEXPRESS` |
| **Target Database Name** | `StaffAutomationDb` |
| **Authentication Mode** | `Integrated Security=True` (Windows Authentication) |
| **Encryption Settings** | `Encrypt=False; TrustServerCertificate=True` |
| **Connection Timeout** | `15 seconds` |
| **Connection Pooling** | `Pooling=True; Max Pool Size=50` |

---

## 4. Mandatory Pre-Migration Safety Backup Protocol (Static Verification)

The Phase 11 DDL script `11_Phase11_Core_Configuration_And_Security_Tables.sql` is additive and non-destructive. However, per production deployment standards, the following SQL Server T-SQL backup command is prepared for execution on the target host:

```sql
BACKUP DATABASE [StaffAutomationDb]
TO DISK = N'E:\Staff automation\database\backups\StaffAutomationDb_PrePhase11_SafetyBackup_20260823.bak'
WITH FORMAT, INIT, NAME = N'StaffAutomationDb Pre-Phase 11 Safety Backup', COMPRESSION, STATS = 10;
GO

RESTORE VERIFYONLY
FROM DISK = N'E:\Staff automation\database\backups\StaffAutomationDb_PrePhase11_SafetyBackup_20260823.bak';
GO
```

---

## 5. Post-Migration Schema Verification Specifications

When executed on the live SQL Server instance, the post-migration check will verify the following object counts:

1. **`dbo.tbl_FirmProfile`**: 1 record (`FirmId = 1`) with `CK_tbl_FirmProfile_SingleRow` constraint.
2. **`dbo.tbl_Permissions`**: 25 catalog permissions grouped across 8 modules:
   - `User Management`: 5 permissions (`USER_VIEW`, `USER_CREATE`, `USER_EDIT`, `USER_RESET_PASSWORD`, `USER_DEACTIVATE`)
   - `Client Master`: 4 permissions (`CLIENT_VIEW`, `CLIENT_CREATE`, `CLIENT_EDIT`, `CLIENT_ARCHIVE`)
   - `Attendance`: 3 permissions (`ATTENDANCE_VIEW`, `ATTENDANCE_EDIT`, `ATTENDANCE_APPROVE`)
   - `Disaster Recovery`: 4 permissions (`BACKUP_VIEW`, `BACKUP_CREATE`, `BACKUP_RESTORE`, `BACKUP_CONFIGURE`)
   - `Firm Settings`: 2 permissions (`FIRM_PROFILE_VIEW`, `FIRM_PROFILE_EDIT`)
   - `Financial Year`: 3 permissions (`FINANCIAL_YEAR_VIEW`, `FINANCIAL_YEAR_MANAGE`, `FINANCIAL_YEAR_LOCK`)
   - `Role Security`: 2 permissions (`ROLE_SECURITY_VIEW`, `ROLE_SECURITY_MANAGE`)
   - `Compliance Audit`: 2 permissions (`AUDIT_LOG_VIEW`, `AUDIT_LOG_EXPORT`)
3. **`dbo.tbl_RolePermissions`**:
   - `Admin Role`: 25 permissions mapped
   - `Owner Role`: 14 permissions mapped
   - `Employee Role`: 4 permissions mapped
4. **`dbo.tbl_UserPermissionOverrides`**: Schema created with `FK_tbl_UserOverrides_tbl_Users` and `FK_tbl_UserOverrides_tbl_Permissions`.
5. **`dbo.tbl_FinancialYears`**: `IsLocked`, `IsDeleted`, `ModifiedOn`, `ModifiedBy` columns present.

---

## 6. Manual Execution Instructions for System Administrator

To complete the live database migration and runtime smoke test on the host machine:

1. **Open PowerShell as Administrator** and navigate to `E:\Staff automation`.
2. **Execute Database Migration via sqlcmd**:
   ```cmd
   sqlcmd -S .\SQLEXPRESS -d StaffAutomationDb -i "database\migrations\11_Phase11_Core_Configuration_And_Security_Tables.sql"
   ```
3. **Run Application Build & Tests**:
   ```cmd
   dotnet build StaffAutomation.sln -c Debug
   dotnet test tests\StaffAutomation.Tests\StaffAutomation.Tests.vbproj -c Debug
   ```
4. **Launch Application**:
   ```cmd
   dotnet run --project src\StaffAutomation.WinForms\StaffAutomation.WinForms.vbproj
   ```
