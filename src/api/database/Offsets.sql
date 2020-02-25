USE [Exam]

GO

CREATE TABLE [Offsets]
(
    [ID] INT PRIMARY KEY NOT NULL,
    [OffsetValue] BIGINT NOT NULL,
)

GO

INSERT INTO  [Offsets] (ID, OffsetValue)
VALUES(1,0)

