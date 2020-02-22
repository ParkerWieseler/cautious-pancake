USE [RandomCode]

GO

Create Table [dbo].[Codes]
(
    [ID] INT PRIMARY KEY IDENTITY,
    [SeedValue] INT UNIQUE NOT NULL,
    [State] TINYINT NOT NULL
);