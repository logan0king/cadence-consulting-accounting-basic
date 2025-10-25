-- Report Designer Database Setup Script
-- This script creates the report designer tables manually

-- Report definitions
CREATE TABLE ReportDefinitions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    ReportGroup NVARCHAR(100) DEFAULT 'General',
    PrintDateUDF NVARCHAR(100),
    AllowedUserGroups NVARCHAR(MAX), -- JSON array
    IsActive BIT DEFAULT 1,
    IsTemplate BIT DEFAULT 0,
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ReportDefinitions_Users FOREIGN KEY (CreatedBy) 
        REFERENCES Users(Id) ON DELETE NO ACTION
);

-- Data sources used in reports
CREATE TABLE ReportDataSources (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReportId UNIQUEIDENTIFIER NOT NULL,
    SourceName NVARCHAR(100) NOT NULL, -- Table/view name
    SourceType NVARCHAR(20) NOT NULL, -- 'table' or 'view'
    Alias NVARCHAR(50),
    PositionX INT DEFAULT 100,
    PositionY INT DEFAULT 100,
    IsSelected BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ReportDataSources_Reports FOREIGN KEY (ReportId) 
        REFERENCES ReportDefinitions(Id) ON DELETE CASCADE
);

-- Selected fields for each data source
CREATE TABLE ReportFields (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReportDataSourceId UNIQUEIDENTIFIER NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    DisplayName NVARCHAR(100),
    IsSelected BIT DEFAULT 0,
    SortOrder INT DEFAULT 0,
    DataType NVARCHAR(50),
    FormatString NVARCHAR(100),
    IsGroupBy BIT DEFAULT 0,
    IsSortBy BIT DEFAULT 0,
    SortDirection NVARCHAR(10) DEFAULT 'ASC',
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ReportFields_DataSources FOREIGN KEY (ReportDataSourceId) 
        REFERENCES ReportDataSources(Id) ON DELETE CASCADE
);

-- Relationships between data sources
CREATE TABLE ReportRelationships (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReportId UNIQUEIDENTIFIER NOT NULL,
    FromDataSourceId UNIQUEIDENTIFIER NOT NULL,
    ToDataSourceId UNIQUEIDENTIFIER NOT NULL,
    FromField NVARCHAR(100) NOT NULL,
    ToField NVARCHAR(100) NOT NULL,
    JoinType NVARCHAR(20) DEFAULT 'INNER',
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ReportRelationships_Reports FOREIGN KEY (ReportId) 
        REFERENCES ReportDefinitions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ReportRelationships_FromSource FOREIGN KEY (FromDataSourceId) 
        REFERENCES ReportDataSources(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_ReportRelationships_ToSource FOREIGN KEY (ToDataSourceId) 
        REFERENCES ReportDataSources(Id) ON DELETE NO ACTION
);

-- Report layout components with comprehensive properties
CREATE TABLE ReportComponents (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReportId UNIQUEIDENTIFIER NOT NULL,
    ComponentType NVARCHAR(50) NOT NULL, -- 'textbox', 'table', 'image', 'line', 'rectangle'
    PositionX INT NOT NULL DEFAULT 0,
    PositionY INT NOT NULL DEFAULT 0,
    Width INT NOT NULL DEFAULT 100,
    Height INT NOT NULL DEFAULT 50,
    Properties NVARCHAR(MAX), -- JSON properties (all categories)
    DataBinding NVARCHAR(MAX), -- JSON data binding info
    ZIndex INT DEFAULT 0,
    IsVisible BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ReportComponents_Reports FOREIGN KEY (ReportId) 
        REFERENCES ReportDefinitions(Id) ON DELETE CASCADE
);

-- Report filters
CREATE TABLE ReportFilters (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReportId UNIQUEIDENTIFIER NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    Operator NVARCHAR(20) NOT NULL,
    FilterValue NVARCHAR(500),
    DataType NVARCHAR(50),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ReportFilters_Reports FOREIGN KEY (ReportId) 
        REFERENCES ReportDefinitions(Id) ON DELETE CASCADE
);

-- Report execution history
CREATE TABLE ReportExecutions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReportId UNIQUEIDENTIFIER NOT NULL,
    ExecutedBy UNIQUEIDENTIFIER NOT NULL,
    ExecutionDate DATETIME2 DEFAULT GETUTCDATE(),
    Parameters NVARCHAR(MAX),
    ExecutionTimeMs INT,
    RecordCount INT,
    Status NVARCHAR(20) DEFAULT 'Success',
    ErrorMessage NVARCHAR(MAX),
    
    CONSTRAINT FK_ReportExecutions_Reports FOREIGN KEY (ReportId) 
        REFERENCES ReportDefinitions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ReportExecutions_Users FOREIGN KEY (ExecutedBy) 
        REFERENCES Users(Id) ON DELETE NO ACTION
);

-- Create indexes for performance
CREATE INDEX IX_ReportDefinitions_Title ON ReportDefinitions(Title);
CREATE INDEX IX_ReportDefinitions_ReportGroup ON ReportDefinitions(ReportGroup);
CREATE INDEX IX_ReportDefinitions_IsActive ON ReportDefinitions(IsActive);
CREATE INDEX IX_ReportDefinitions_CreatedBy ON ReportDefinitions(CreatedBy);

CREATE INDEX IX_ReportDataSources_ReportId ON ReportDataSources(ReportId);
CREATE INDEX IX_ReportDataSources_SourceName ON ReportDataSources(SourceName);

CREATE INDEX IX_ReportFields_ReportDataSourceId ON ReportFields(ReportDataSourceId);
CREATE INDEX IX_ReportFields_FieldName ON ReportFields(FieldName);

CREATE INDEX IX_ReportRelationships_ReportId ON ReportRelationships(ReportId);
CREATE INDEX IX_ReportRelationships_FromDataSourceId ON ReportRelationships(FromDataSourceId);
CREATE INDEX IX_ReportRelationships_ToDataSourceId ON ReportRelationships(ToDataSourceId);

CREATE INDEX IX_ReportComponents_ReportId ON ReportComponents(ReportId);
CREATE INDEX IX_ReportComponents_ComponentType ON ReportComponents(ComponentType);

CREATE INDEX IX_ReportFilters_ReportId ON ReportFilters(ReportId);
CREATE INDEX IX_ReportFilters_FieldName ON ReportFilters(FieldName);

CREATE INDEX IX_ReportExecutions_ReportId ON ReportExecutions(ReportId);
CREATE INDEX IX_ReportExecutions_ExecutedBy ON ReportExecutions(ExecutedBy);
CREATE INDEX IX_ReportExecutions_ExecutionDate ON ReportExecutions(ExecutionDate);