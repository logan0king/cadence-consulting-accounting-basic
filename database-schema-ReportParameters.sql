-- Add ReportParameters and ReportParameterOptions tables
-- Run this if you need to add these tables to an existing database

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReportParameters')
BEGIN
    CREATE TABLE [ReportParameters] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [ReportId] UNIQUEIDENTIFIER NOT NULL,
        [ParameterName] NVARCHAR(100) NOT NULL,
        [ParameterType] INT NOT NULL,
        [Prompt] NVARCHAR(500) NULL,
        [DefaultValue] NVARCHAR(MAX) NULL,
        [Required] BIT NOT NULL DEFAULT 0,
        [LookupSource] NVARCHAR(MAX) NULL,
        [DisplayField] NVARCHAR(100) NULL,
        [ValueField] NVARCHAR(100) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        FOREIGN KEY ([ReportId]) REFERENCES [ReportDefinitions]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ReportParameters_ReportId] ON [ReportParameters]([ReportId]);
    CREATE INDEX [IX_ReportParameters_ParameterName] ON [ReportParameters]([ParameterName]);
    
    PRINT 'ReportParameters table created successfully';
END
ELSE
BEGIN
    PRINT 'ReportParameters table already exists';
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReportParameterOptions')
BEGIN
    CREATE TABLE [ReportParameterOptions] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [ParameterId] UNIQUEIDENTIFIER NOT NULL,
        [DisplayValue] NVARCHAR(200) NULL,
        [ActualValue] NVARCHAR(200) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        
        FOREIGN KEY ([ParameterId]) REFERENCES [ReportParameters]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ReportParameterOptions_ParameterId] ON [ReportParameterOptions]([ParameterId]);
    
    PRINT 'ReportParameterOptions table created successfully';
END
ELSE
BEGIN
    PRINT 'ReportParameterOptions table already exists';
END

