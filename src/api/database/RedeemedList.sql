USE [Exam]

Go

CREATE TABLE [RedeemedList]
(
    ID INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
    CodeSeedValue INT NOT NULL,
    Email VARCHAR(100) NOT NULL
)

GO