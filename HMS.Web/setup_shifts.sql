-- Create UserShifts table for tracking Cashier/Admin sessions
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserShifts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserShifts](
        [ShiftId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] [nvarchar](450) NOT NULL, -- Identity User Id
        [StartTime] [datetime] NOT NULL DEFAULT GETDATE(),
        [EndTime] [datetime] NULL,
        [StartingCash] [decimal](18, 2) NOT NULL DEFAULT 0,
        [EndingCash] [decimal](18, 2) NULL, -- System Calculated
        [ActualCash] [decimal](18, 2) NULL, -- User Declared
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Open', -- Open, Closed
        [Notes] [nvarchar](max) NULL
    );
END

-- Update Bills table to include Shift tagging
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND name = 'ShiftId')
BEGIN
    ALTER TABLE [dbo].[Bills] ADD [ShiftId] [int] NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE [dbo].[Bills] ADD [CreatedBy] [nvarchar](450) NULL;
END
