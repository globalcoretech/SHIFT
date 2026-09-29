USE [StaffAutomationDb]
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_TaskChecklistItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_TaskChecklistItems](
        [ChecklistId] [int] IDENTITY(1,1) NOT NULL,
        [TaskId] [int] NOT NULL,
        [ItemDescription] [nvarchar](max) NOT NULL,
        [RequiresProof] [bit] NOT NULL CONSTRAINT [DF_tbl_TaskChecklistItems_RequiresProof] DEFAULT ((0)),
        [IsCompleted] [bit] NOT NULL CONSTRAINT [DF_tbl_TaskChecklistItems_IsCompleted] DEFAULT ((0)),
        [CompletedByUserId] [int] NULL,
        [CompletedOn] [datetime] NULL,
        [Remarks] [nvarchar](max) NULL,
        [AttachmentPath] [nvarchar](500) NULL,
        [SortOrder] [int] NOT NULL CONSTRAINT [DF_tbl_TaskChecklistItems_SortOrder] DEFAULT ((0)),
        [CreatedOn] [datetime] NOT NULL CONSTRAINT [DF_tbl_TaskChecklistItems_CreatedOn] DEFAULT (getutcdate()),
        CONSTRAINT [PK_tbl_TaskChecklistItems] PRIMARY KEY CLUSTERED ([ChecklistId] ASC)
    )

    ALTER TABLE [dbo].[tbl_TaskChecklistItems]  WITH CHECK ADD  CONSTRAINT [FK_tbl_TaskChecklistItems_tbl_Tasks] FOREIGN KEY([TaskId])
    REFERENCES [dbo].[tbl_Tasks] ([TaskId])
    ON DELETE CASCADE

    ALTER TABLE [dbo].[tbl_TaskChecklistItems] CHECK CONSTRAINT [FK_tbl_TaskChecklistItems_tbl_Tasks]

    ALTER TABLE [dbo].[tbl_TaskChecklistItems]  WITH CHECK ADD  CONSTRAINT [FK_tbl_TaskChecklistItems_tbl_Users] FOREIGN KEY([CompletedByUserId])
    REFERENCES [dbo].[tbl_Users] ([UserId])
    
    ALTER TABLE [dbo].[tbl_TaskChecklistItems] CHECK CONSTRAINT [FK_tbl_TaskChecklistItems_tbl_Users]
END
GO
