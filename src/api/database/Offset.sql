USE [RandomCode]

GO

Create Table [Offset]
(
    [ID] INT PRIMARY KEY IDENTITY,
    [OffsetValue] BIGINT NOT NULL,
);

INSERT INTO Value(ID, OffsetValue)
Values(1,1);