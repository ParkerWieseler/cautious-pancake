USE [RandomCodes]

GO

Create Table [dbo].[Code]
(
    [ID] INT PRIMARY KEY IDENTITY,
    [SeedValue] INT UNIQUE NOT NULL,
    [State] TINYINT NOT NULL
);