# PHASE — GSTIN AUTO-FETCH FOR EXISTING CLIENT REGISTRATION

## 1. Objective
User enters a valid GSTIN -> Clicks "Fetch GST Details" -> Application calls approved GST API -> Receives taxpayer details -> Populates Client form fields for user review -> User clicks Save -> Persists to SQL Server via ClientService.

## 2. Scope & Implementation Boundaries
- **In-Scope**: `FrmAddEditClient.vb`, `GstVerificationEngine.vb`, `App.config`
- **Out-of-Scope**: Attendance, Tasks, Dashboard, Reports, Database schema.

## 3. Tech Stack & Architecture
- **Language**: VB.NET / .NET 8 WinForms
- **Data Parser**: `.NET 8 System.Text.Json`
- **Architecture**: Service Layer (`GstVerificationEngine`) -> Form Handler (`FrmAddEditClient`) -> BLL -> DAL -> SQL Server

## 4. Key Rules Enforced
- **Zero Scraping / Browser / CAPTCHA Automation**.
- **No Fake / Demo Data**: Never populate synthetic fake names or dummy addresses.
- **Data Persistence Safety**: Auto-fetched data loaded into UI controls for review ONLY. Persistence occurs only when user clicks "Save Client".
- **Masked Audit Logging**: GSTIN logged as `27*****0000A1Z5`.

## 5. Verification Status
- **Build Result**: 0 Errors, 0 Warnings
- **Status**: INSTALLED & VERIFIED
