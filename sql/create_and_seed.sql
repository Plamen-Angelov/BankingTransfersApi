-- Banking Transfers API — Database Setup Script
-- Creates the database, tables, indexes, and seeds test data.
-- Run this script against your SQL Server instance before starting the application.

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BankingTransfersDb')
BEGIN
    CREATE DATABASE BankingTransfersDb;
END
GO

USE BankingTransfersDb;
GO

-- ============================================================
-- Tables
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserProfiles')
BEGIN
    CREATE TABLE UserProfiles (
        Id       INT IDENTITY(1,1) NOT NULL,
        UId      UNIQUEIDENTIFIER  NOT NULL,
        Username NVARCHAR(100)     NOT NULL,
        CONSTRAINT PK_UserProfiles PRIMARY KEY (Id)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserProfileAccountPermissions')
BEGIN
    CREATE TABLE UserProfileAccountPermissions (
        Id                       UNIQUEIDENTIFIER NOT NULL,
        UserProfileId            INT              NOT NULL,
        IBAN                     NVARCHAR(34)     NOT NULL,
        CreateTransferPermission BIT              NOT NULL,
        CONSTRAINT PK_UserProfileAccountPermissions PRIMARY KEY (Id),
        CONSTRAINT FK_UserProfileAccountPermissions_UserProfiles
            FOREIGN KEY (UserProfileId) REFERENCES UserProfiles(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransferRequests')
BEGIN
    CREATE TABLE TransferRequests (
        Id                  INT IDENTITY(1,1)  NOT NULL,
        UId                 UNIQUEIDENTIFIER   NOT NULL,
        UserProfileId       INT                NOT NULL,
        SourceIban          NVARCHAR(34)       NOT NULL,
        TargetIban          NVARCHAR(34)       NOT NULL,
        Amount              DECIMAL(18,2)      NOT NULL,
        Currency            NVARCHAR(3)        NOT NULL,
        Reason              NVARCHAR(MAX)      NOT NULL DEFAULT N'',
        ExecutionDate       DATETIME2          NOT NULL,
        Status              NVARCHAR(20)       NOT NULL,
        IdempotencyKey      NVARCHAR(200)      NOT NULL,
        RetryCount          INT                NOT NULL DEFAULT 0,
        ErrorMessage        NVARCHAR(1000)     NULL,
        CreatedAt           DATETIME2          NOT NULL,
        ProcessingStartedAt DATETIME2          NULL,
        ProcessedAt         DATETIME2          NULL,
        LastRetryAt         DATETIME2          NULL,
        CONSTRAINT PK_TransferRequests PRIMARY KEY (Id),
        CONSTRAINT FK_TransferRequests_UserProfiles
            FOREIGN KEY (UserProfileId) REFERENCES UserProfiles(Id) ON DELETE RESTRICT
    );
END
GO

-- ============================================================
-- Indexes
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserProfiles_UId')
    CREATE UNIQUE INDEX IX_UserProfiles_UId
        ON UserProfiles(UId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserProfileAccountPermissions_UserProfileId_IBAN')
    CREATE UNIQUE INDEX IX_UserProfileAccountPermissions_UserProfileId_IBAN
        ON UserProfileAccountPermissions(UserProfileId, IBAN);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TransferRequests_UId')
    CREATE UNIQUE INDEX IX_TransferRequests_UId
        ON TransferRequests(UId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TransferRequests_IdempotencyKey')
    CREATE UNIQUE INDEX IX_TransferRequests_IdempotencyKey
        ON TransferRequests(IdempotencyKey);
GO

-- Index on Status used by the background processor when claiming Pending transfers
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TransferRequests_Status')
    CREATE INDEX IX_TransferRequests_Status
        ON TransferRequests(Status);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TransferRequests_UserProfileId')
    CREATE INDEX IX_TransferRequests_UserProfileId
        ON TransferRequests(UserProfileId);
GO

-- ============================================================
-- Seed data
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM UserProfiles)
BEGIN
    INSERT INTO UserProfiles (UId, Username) VALUES
        ('8d5fa53a-fe3b-4f74-b5c4-7b43dc4e2187', 'john.doe'),
        ('78f90a74-c792-45ca-a6b1-4dac75f4604d', 'jane.smith');

    DECLARE @JohnId INT = (SELECT Id FROM UserProfiles WHERE UId = '8d5fa53a-fe3b-4f74-b5c4-7b43dc4e2187');
    DECLARE @JaneId INT = (SELECT Id FROM UserProfiles WHERE UId = '78f90a74-c792-45ca-a6b1-4dac75f4604d');

    INSERT INTO UserProfileAccountPermissions (Id, UserProfileId, IBAN, CreateTransferPermission) VALUES
        ('01814d3d-d15d-44a8-a128-7d3435faaf06', @JohnId, 'BG12TEST1234567890', 1),
        ('c7eaadfe-867c-4a10-b12f-20a95fdfbb66', @JohnId, 'BG99TEST0000000001', 0),
        ('345fc413-2e60-4d83-9a75-b8d11c3b9b3d', @JaneId, 'BG34TEST9876543210', 1),
        ('f6b47560-5a58-4ea7-bc0b-6f653f003848', @JaneId, 'BG99TEST0000000002', 0);
END
GO
