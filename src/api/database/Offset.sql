USE [RandomCode]

GO

Create Table [dbo].[Offset]
(
    [ID] INT PRIMARY KEY IDENTITY,
    [OffsetValue] BIGINT NOT NULL,
);

INSERT INTO  Offset (ID, OffsetValue)
Values(1,0);