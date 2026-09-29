-- =====================================================================
-- Migration: 12_Phase12_Staff_Device_Registration_Table.sql
-- Description: Creates the dbo.tbl_UserDevices table for Staff-PC Device Registration.
-- Ensures idempotency.
-- =====================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_UserDevices]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_UserDevices] (
        [DeviceId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [DeviceGuid] UNIQUEIDENTIFIER NOT NULL,
        [HardwareFingerprint] NVARCHAR(256) NOT NULL,
        [MachineName] NVARCHAR(100) NOT NULL,
        [OSVersion] NVARCHAR(100) NULL,
        [Status] NVARCHAR(20) NOT NULL,
        [RequestedOn] DATETIME2 NOT NULL CONSTRAINT [DF_tbl_UserDevices_RequestedOn] DEFAULT GETUTCDATE(),
        [ApprovedOn] DATETIME2 NULL,
        [ApprovedBy] INT NULL,
        [RevokedOn] DATETIME2 NULL,
        [RevokedBy] INT NULL,
        [RevocationReason] NVARCHAR(500) NULL,
        [LastActiveOn] DATETIME2 NULL,

        CONSTRAINT [PK_tbl_UserDevices] PRIMARY KEY CLUSTERED ([DeviceId] ASC),
        
        CONSTRAINT [FK_tbl_UserDevices_UserId] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[tbl_Users] ([UserId]),
            
        CONSTRAINT [FK_tbl_UserDevices_ApprovedBy] FOREIGN KEY ([ApprovedBy])
            REFERENCES [dbo].[tbl_Users] ([UserId]),
            
        CONSTRAINT [FK_tbl_UserDevices_RevokedBy] FOREIGN KEY ([RevokedBy])
            REFERENCES [dbo].[tbl_Users] ([UserId]),
            
        CONSTRAINT [CHK_tbl_UserDevices_Status] CHECK ([Status] IN ('Pending', 'Approved', 'Rejected', 'Revoked')),
        
        -- A single user can only have one registration per DeviceGuid and HardwareFingerprint
        CONSTRAINT [UQ_tbl_UserDevices_UserId_DeviceGuid] UNIQUE ([UserId], [DeviceGuid]),
        CONSTRAINT [UQ_tbl_UserDevices_UserId_HardwareFingerprint] UNIQUE ([UserId], [HardwareFingerprint])
    );

    CREATE NONCLUSTERED INDEX [IX_tbl_UserDevices_UserId] ON [dbo].[tbl_UserDevices] ([UserId]);
    CREATE NONCLUSTERED INDEX [IX_tbl_UserDevices_Status] ON [dbo].[tbl_UserDevices] ([Status]);
END
GO
