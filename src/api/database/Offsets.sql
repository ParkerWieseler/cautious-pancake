USE [RandomCode]

GO

Create Table [dbo].[Offsets]
(
    [ID] INT PRIMARY KEY IDENTITY,
    [OffsetValue] BIGINT NOT NULL,
)

GO

INSERT INTO  [Offsets] (ID, OffsetValue)
Values(1,0);