-- Migration 15: Make Client Department Nullable
-- This migration safely changes the DepartmentId column in dbo.tbl_Clients from NOT NULL to NULL
-- It preserves the existing foreign key constraint if it exists.

IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_Clients') 
    AND name = 'DepartmentId' 
    AND is_nullable = 0
)
BEGIN
    PRINT 'Altering dbo.tbl_Clients.DepartmentId to allow NULL...'
    ALTER TABLE dbo.tbl_Clients ALTER COLUMN DepartmentId INT NULL;
    PRINT 'Successfully altered dbo.tbl_Clients.DepartmentId to allow NULL.'
END
ELSE
BEGIN
    PRINT 'dbo.tbl_Clients.DepartmentId is already NULLable or column does not exist.'
END
