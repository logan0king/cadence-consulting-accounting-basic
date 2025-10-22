-- Cadence Consulting Accounting Basic - Database Setup Script
-- Execute this script against your SQL Server Express instance

-- Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CadenceAccounting')
BEGIN
    CREATE DATABASE [CadenceAccounting]
    COLLATE SQL_Latin1_General_CP1_CI_AS
END
GO

USE [CadenceAccounting]
GO

-- Create Tables

-- Users Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE [dbo].[Users] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Username] NVARCHAR(50) NOT NULL UNIQUE,
    [Email] NVARCHAR(255) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [Role] NVARCHAR(50) DEFAULT 'Admin',
    [IsActive] BIT DEFAULT 1,
    [EmailVerified] BIT DEFAULT 0,
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [LastLogin] DATETIME2 NULL
)
GO

-- Companies Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Companies' AND xtype='U')
CREATE TABLE [dbo].[Companies] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL,
    [TaxId] NVARCHAR(50) NULL,
    [Address] NVARCHAR(500) NULL,
    [City] NVARCHAR(100) NULL,
    [State] NVARCHAR(50) NULL,
    [ZipCode] NVARCHAR(20) NULL,
    [Phone] NVARCHAR(20) NULL,
    [Email] NVARCHAR(255) NULL,
    [Website] NVARCHAR(255) NULL,
    [LogoPath] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 DEFAULT GETUTCDATE()
)
GO

-- Projects Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Projects' AND xtype='U')
CREATE TABLE [dbo].[Projects] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [CompanyId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [ProjectNumber] NVARCHAR(50) NOT NULL,
    [ClientName] NVARCHAR(255) NOT NULL,
    [ClientEmail] NVARCHAR(255) NULL,
    [ClientPhone] NVARCHAR(20) NULL,
    [ClientAddress] NVARCHAR(500) NULL,
    [Budget] DECIMAL(15,2) NOT NULL,
    [HourlyRate] DECIMAL(10,2) NOT NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NULL,
    [Status] NVARCHAR(50) DEFAULT 'Active',
    [TaxType] NVARCHAR(50) DEFAULT '1099',
    [ContractDocument] VARBINARY(MAX) NULL,
    [ContractText] NVARCHAR(MAX) NULL,
    [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([Id]),
    FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id])
)
GO

-- Time Entries Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TimeEntries' AND xtype='U')
CREATE TABLE [dbo].[TimeEntries] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ProjectId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Date] DATE NOT NULL,
    [Hours] DECIMAL(5,2) NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [Rate] DECIMAL(10,2) NOT NULL,
    [Amount] DECIMAL(15,2) NOT NULL,
    [IsBillable] BIT DEFAULT 1,
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]),
    FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
)
GO

-- Expenses Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Expenses' AND xtype='U')
CREATE TABLE [dbo].[Expenses] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ProjectId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Date] DATE NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [Amount] DECIMAL(15,2) NOT NULL,
    [Category] NVARCHAR(100) NOT NULL,
    [ReceiptImage] VARBINARY(MAX) NULL,
    [ReceiptFileName] NVARCHAR(255) NULL,
    [IsBillable] BIT DEFAULT 1,
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]),
    FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
)
GO

-- Invoices Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Invoices' AND xtype='U')
CREATE TABLE [dbo].[Invoices] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ProjectId] UNIQUEIDENTIFIER NOT NULL,
    [InvoiceNumber] NVARCHAR(100) NOT NULL UNIQUE,
    [InvoiceDate] DATE NOT NULL,
    [DueDate] DATE NOT NULL,
    [Subtotal] DECIMAL(15,2) NOT NULL,
    [TaxAmount] DECIMAL(15,2) DEFAULT 0,
    [TotalAmount] DECIMAL(15,2) NOT NULL,
    [Status] NVARCHAR(50) DEFAULT 'Draft',
    [Notes] NVARCHAR(MAX) NULL,
    [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]),
    FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id])
)
GO

-- Invoice Items Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InvoiceItems' AND xtype='U')
CREATE TABLE [dbo].[InvoiceItems] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [Quantity] DECIMAL(10,2) NOT NULL,
    [Rate] DECIMAL(15,2) NOT NULL,
    [Amount] DECIMAL(15,2) NOT NULL,
    [ItemType] NVARCHAR(50) NOT NULL, -- 'Time' or 'Expense'
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices]([Id])
)
GO

-- Payments Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Payments' AND xtype='U')
CREATE TABLE [dbo].[Payments] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
    [Amount] DECIMAL(15,2) NOT NULL,
    [PaymentDate] DATE NOT NULL,
    [PaymentMethod] NVARCHAR(50) NOT NULL,
    [Reference] NVARCHAR(100) NULL,
    [BankTransactionId] NVARCHAR(100) NULL,
    [Notes] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices]([Id])
)
GO

-- System Settings Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SystemSettings' AND xtype='U')
CREATE TABLE [dbo].[SystemSettings] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Key] NVARCHAR(100) NOT NULL UNIQUE,
    [Value] NVARCHAR(MAX) NULL,
    [Description] NVARCHAR(500) NULL,
    [Category] NVARCHAR(50) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 DEFAULT GETUTCDATE()
)
GO

-- Audit Logs Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
CREATE TABLE [dbo].[AuditLogs] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [TableName] NVARCHAR(100) NOT NULL,
    [RecordId] UNIQUEIDENTIFIER NOT NULL,
    [Action] NVARCHAR(20) NOT NULL, -- 'INSERT', 'UPDATE', 'DELETE'
    [OldValues] NVARCHAR(MAX) NULL,
    [NewValues] NVARCHAR(MAX) NULL,
    [ChangedBy] UNIQUEIDENTIFIER NOT NULL,
    [ChangedAt] DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY ([ChangedBy]) REFERENCES [Users]([Id])
)
GO

-- Project Templates Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProjectTemplates' AND xtype='U')
CREATE TABLE [dbo].[ProjectTemplates] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [DefaultTaxType] NVARCHAR(50) DEFAULT '1099',
    [DefaultHourlyRate] DECIMAL(10,2) NOT NULL,
    [DefaultBudget] DECIMAL(15,2) NULL,
    [CommonExpenses] NVARCHAR(MAX) NULL, -- JSON array of expense categories
    [CreatedAt] DATETIME2 DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 DEFAULT GETUTCDATE()
)
GO

-- Create Indexes for Performance
CREATE INDEX IX_Users_Email ON [Users]([Email])
CREATE INDEX IX_Users_Username ON [Users]([Username])
CREATE INDEX IX_Projects_CompanyId ON [Projects]([CompanyId])
CREATE INDEX IX_Projects_Status ON [Projects]([Status])
CREATE INDEX IX_TimeEntries_ProjectId ON [TimeEntries]([ProjectId])
CREATE INDEX IX_TimeEntries_Date ON [TimeEntries]([Date])
CREATE INDEX IX_Expenses_ProjectId ON [Expenses]([ProjectId])
CREATE INDEX IX_Expenses_Date ON [Expenses]([Date])
CREATE INDEX IX_Invoices_ProjectId ON [Invoices]([ProjectId])
CREATE INDEX IX_Invoices_Status ON [Invoices]([Status])
CREATE INDEX IX_InvoiceItems_InvoiceId ON [InvoiceItems]([InvoiceId])
CREATE INDEX IX_Payments_InvoiceId ON [Payments]([InvoiceId])
CREATE INDEX IX_AuditLogs_TableRecord ON [AuditLogs]([TableName], [RecordId])
CREATE INDEX IX_AuditLogs_ChangedAt ON [AuditLogs]([ChangedAt])

-- Create Triggers for UpdatedAt
GO
CREATE TRIGGER TR_Users_UpdatedAt ON [Users] AFTER UPDATE AS
BEGIN
    UPDATE [Users] SET [UpdatedAt] = GETUTCDATE() WHERE [Id] IN (SELECT [Id] FROM inserted)
END
GO

CREATE TRIGGER TR_Companies_UpdatedAt ON [Companies] AFTER UPDATE AS
BEGIN
    UPDATE [Companies] SET [UpdatedAt] = GETUTCDATE() WHERE [Id] IN (SELECT [Id] FROM inserted)
END
GO

CREATE TRIGGER TR_Projects_UpdatedAt ON [Projects] AFTER UPDATE AS
BEGIN
    UPDATE [Projects] SET [UpdatedAt] = GETUTCDATE() WHERE [Id] IN (SELECT [Id] FROM inserted)
END
GO

CREATE TRIGGER TR_TimeEntries_UpdatedAt ON [TimeEntries] AFTER UPDATE AS
BEGIN
    UPDATE [TimeEntries] SET [UpdatedAt] = GETUTCDATE() WHERE [Id] IN (SELECT [Id] FROM inserted)
END
GO

CREATE TRIGGER TR_Expenses_UpdatedAt ON [Expenses] AFTER UPDATE AS
BEGIN
    UPDATE [Expenses] SET [UpdatedAt] = GETUTCDATE() WHERE [Id] IN (SELECT [Id] FROM inserted)
END
GO

CREATE TRIGGER TR_Invoices_UpdatedAt ON [Invoices] AFTER UPDATE AS
BEGIN
    UPDATE [Invoices] SET [UpdatedAt] = GETUTCDATE() WHERE [Id] IN (SELECT [Id] FROM inserted)
END
GO

-- Insert Default Data
-- Default Admin User (Password: Admin123!)
INSERT INTO [Users] ([Username], [Email], [PasswordHash], [FirstName], [LastName], [Role], [IsActive], [EmailVerified])
VALUES ('admin', 'admin@cadence-consulting.com', 'AQAAAAEAACcQAAAAEHashPasswordHere', 'Admin', 'User', 'Admin', 1, 1)

-- Default Company
INSERT INTO [Companies] ([Name], [TaxId], [Email], [Phone])
VALUES ('Cadence Consulting LLC', '12-3456789', 'info@cadence-consulting.com', '(312) 555-0123')

-- Default System Settings
INSERT INTO [SystemSettings] ([Key], [Value], [Description], [Category])
VALUES 
('InvoiceNumberFormat', 'INV-{ProjectNumber}-{Year}-{SequentialNumber}', 'Invoice number format template', 'Invoice'),
('DefaultTaxType', '1099', 'Default tax type for new projects', 'Tax'),
('FederalWithholdingRate', '15.0', 'Federal income tax withholding rate', 'Tax'),
('IllinoisStateTaxRate', '4.95', 'Illinois state tax rate', 'Tax'),
('SocialSecurityRate', '6.2', 'Social Security tax rate', 'Tax'),
('MedicareRate', '1.45', 'Medicare tax rate', 'Tax'),
('DefaultHourlyRate', '75.00', 'Default hourly rate for projects', 'Project'),
('CompanyName', 'Cadence Consulting LLC', 'Company name for invoices', 'Company'),
('CompanyEmail', 'info@cadence-consulting.com', 'Company email for invoices', 'Company'),
('SMTP_Server', 'smtp.office365.com', 'SMTP server for email', 'Email'),
('SMTP_Port', '587', 'SMTP port for email', 'Email'),
('Backup_Frequency', 'Daily', 'Backup frequency', 'Backup'),
('Backup_Location', 'C:\Backups\CadenceAccounting', 'Backup location', 'Backup'),
('Theme', 'Journal', 'Default theme', 'UI'),
('SessionTimeout', '30', 'Session timeout in minutes', 'Security')

-- Default Project Template
INSERT INTO [ProjectTemplates] ([Name], [Description], [DefaultTaxType], [DefaultHourlyRate], [DefaultBudget], [CommonExpenses])
VALUES ('State Contract', 'Template for state government contracts', '1099', 75.00, 50000.00, '["Travel", "Meals", "Supplies", "Equipment", "Software"]')

PRINT 'Database setup completed successfully!'
PRINT 'Default admin user created: admin / Admin123!'
PRINT 'Default company created: Cadence Consulting LLC'
PRINT 'System settings initialized'
