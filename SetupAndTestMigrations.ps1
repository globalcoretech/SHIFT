New-Item -ItemType Directory -Path C:\Temp -Force | Out-Null
$MigrationsDir = "e:\Staff automation\src\StaffAutomation.DAL\Migrations"
if (!(Test-Path $MigrationsDir)) { New-Item -ItemType Directory -Path $MigrationsDir }

$masterContent = Get-Content "e:\Staff automation\database\scripts\00_Master_Complete_Schema_And_Seed.sql" -Raw
$masterContent = $masterContent -replace "(?si)USE \[master\];.*?USE \[StaffAutomationDb\];\s*GO\s*", ""
Set-Content -Path "$MigrationsDir\001_Master_Complete_Schema_And_Seed.sql" -Value $masterContent -Encoding UTF8

Copy-Item "e:\Staff automation\database\scripts\01_CreateTaskChecklistItems.sql" "$MigrationsDir\002_CreateTaskChecklistItems.sql" -Force
Copy-Item "e:\Staff automation\database\scripts\07_Add_BreakStartTime_To_tbl_Attendance.sql" "$MigrationsDir\003_Add_BreakStartTime_To_tbl_Attendance.sql" -Force
Copy-Item "e:\Staff automation\database\scripts\08_Add_Admin_Correction_Columns_To_tbl_Attendance.sql" "$MigrationsDir\004_Add_Admin_Correction_Columns_To_tbl_Attendance.sql" -Force
Copy-Item "e:\Staff automation\database\scripts\08_Database_Backup_And_Integrity_Tables.sql" "$MigrationsDir\005_Database_Backup_And_Integrity_Tables.sql" -Force
Copy-Item "e:\Staff automation\database\scripts\09_Add_MustChangePassword_To_tbl_Users.sql" "$MigrationsDir\006_Add_MustChangePassword_To_tbl_Users.sql" -Force
Copy-Item "e:\Staff automation\database\scripts\10_Add_Client_Gst_Metadata_Columns.sql" "$MigrationsDir\007_Add_Client_Gst_Metadata_Columns.sql" -Force
Copy-Item "e:\Staff automation\database\migrations\11_Phase11_Core_Configuration_And_Security_Tables.sql" "$MigrationsDir\008_Phase11_Core_Configuration_And_Security_Tables.sql" -Force
Copy-Item "e:\Staff automation\database\migrations\12_Phase12_DisasterRecovery_And_Backup_Tables.sql" "$MigrationsDir\009_Phase12_DisasterRecovery_And_Backup_Tables.sql" -Force
Copy-Item "e:\Staff automation\database\migrations\12_Phase12_Staff_Device_Registration_Table.sql" "$MigrationsDir\010_Phase12_Staff_Device_Registration_Table.sql" -Force
Copy-Item "e:\Staff automation\database\migrations\13_Phase12_Compliance_And_Audit_Tables.sql" "$MigrationsDir\011_Phase12_Compliance_And_Audit_Tables.sql" -Force

Set-Content -Path "$MigrationsDir\012_Phase13_DeviceRegistration_Duplicate_Skipped.sql" -Value "-- Skipped (Duplicate of 010)" -Encoding UTF8
Set-Content -Path "$MigrationsDir\013_Phase14_Placeholder.sql" -Value "-- Placeholder" -Encoding UTF8
Set-Content -Path "$MigrationsDir\014_Phase15_Placeholder.sql" -Value "-- Placeholder" -Encoding UTF8
Copy-Item "e:\Staff automation\database\migrations\15_Make_Client_Department_Nullable.sql" "$MigrationsDir\015_Make_Client_Department_Nullable.sql" -Force
Set-Content -Path "$MigrationsDir\016_System_SchemaHistory.sql" -Value "-- Schema history logic is natively managed by MigrationRunner.vb" -Encoding UTF8

Write-Host "1. Migrations folder populated successfully with 16 scripts." -ForegroundColor Green

Write-Host "2. Backing up existing StaffAutomationDb..." -ForegroundColor Yellow
sqlcmd -S localhost\SQLEXPRESS -E -Q "BACKUP DATABASE [StaffAutomationDb] TO DISK = 'C:\Temp\StaffAutomationDb.bak' WITH FORMAT, INIT;"

Write-Host "3. Creating copy StaffAutomationDb_Copy..." -ForegroundColor Yellow
sqlcmd -S localhost\SQLEXPRESS -E -Q "RESTORE DATABASE [StaffAutomationDb_Copy] FROM DISK = 'C:\Temp\StaffAutomationDb.bak' WITH MOVE 'StaffAutomationDb' TO 'C:\Temp\StaffAutomationDb_Copy.mdf', MOVE 'StaffAutomationDb_log' TO 'C:\Temp\StaffAutomationDb_Copy_log.ldf', REPLACE;"

Write-Host "4. Creating fresh StaffAutomationDb_Fresh..." -ForegroundColor Yellow
sqlcmd -S localhost\SQLEXPRESS -E -Q "IF EXISTS (SELECT name FROM sys.databases WHERE name = N'StaffAutomationDb_Fresh') DROP DATABASE [StaffAutomationDb_Fresh]; CREATE DATABASE [StaffAutomationDb_Fresh];"

Write-Host "Done! You can now test the MigrationRunner by pointing dbconnection.json to 'StaffAutomationDb_Copy' and then 'StaffAutomationDb_Fresh' and running the application." -ForegroundColor Cyan
