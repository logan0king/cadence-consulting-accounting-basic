-- Update ReportFilters table schema to match enhanced filter model
-- This script adds new columns and modifies existing ones

USE [CadenceAccounting];
GO

-- Check if we need to modify the table
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND type in (N'U'))
BEGIN
    PRINT 'Updating ReportFilters table schema...';
    
    -- Drop old columns if they exist
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'DataType')
    BEGIN
        ALTER TABLE [dbo].[ReportFilters] DROP COLUMN [DataType];
        PRINT 'Dropped column DataType';
    END

    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'IsActive')
    BEGIN
        ALTER TABLE [dbo].[ReportFilters] DROP COLUMN [IsActive];
        PRINT 'Dropped column IsActive';
    END

    -- Rename FieldName to TableName if needed
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'FieldName')
    BEGIN
        EXEC sp_rename '[dbo].[ReportFilters].[FieldName]', 'TableName', 'COLUMN';
        PRINT 'Renamed column FieldName to TableName';
    END

    -- Drop old index if exists
    IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'IX_ReportFilters_FieldName')
    BEGIN
        DROP INDEX [IX_ReportFilters_FieldName] ON [dbo].[ReportFilters];
        PRINT 'Dropped index IX_ReportFilters_FieldName';
    END

    -- Modify Operator column from nvarchar(20) to int if needed
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'Operator')
    BEGIN
        ALTER TABLE [dbo].[ReportFilters] ALTER COLUMN [Operator] int NOT NULL;
        PRINT 'Altered Operator column to int';
    END

    -- Add new columns
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'ColumnName')
    BEGIN
        ALTER TABLE [dbo].[ReportFilters] ADD [ColumnName] nvarchar(100) NOT NULL DEFAULT '';
        PRINT 'Added column ColumnName';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'LogicOperator')
    BEGIN
        ALTER TABLE [dbo].[ReportFilters] ADD [LogicOperator] int NULL;
        PRINT 'Added column LogicOperator';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'SortOrder')
    BEGIN
        ALTER TABLE [dbo].[ReportFilters] ADD [SortOrder] int NOT NULL DEFAULT 0;
        PRINT 'Added column SortOrder';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'UpdatedAt')
    BEGIN
        ALTER TABLE [dbo].[ReportFilters] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE();
        PRINT 'Added column UpdatedAt';
    END

    -- Create new indexes
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'IX_ReportFilters_TableName')
    BEGIN
        CREATE INDEX [IX_ReportFilters_TableName] ON [dbo].[ReportFilters] ([TableName]);
        PRINT 'Created index IX_ReportFilters_TableName';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ReportFilters]') AND name = 'IX_ReportFilters_ColumnName')
    BEGIN
        CREATE INDEX [IX_ReportFilters_ColumnName] ON [dbo].[ReportFilters] ([ColumnName]);
        PRINT 'Created index IX_ReportFilters_ColumnName';
    END

    PRINT 'ReportFilters table schema updated successfully';
END
ELSE
BEGIN
    PRINT 'ReportFilters table does not exist. Migration will be handled by EF Core.';
END
GO

