Use [Exam]

GO


CREATE TABLE [Promotion]
(
    [ID] INT PRIMARY KEY IDENTITY (1,1),
    [PromotionName] VARCHAR(50) NOT NULL,
    [CodeIDStart] INT UNIQUE NOT NULL,
    [CodeIDEnd] AS [CodeIDStart] + [PromotionSize] -1,
    [PromotionSize] INT NOT NULL
   
)
GO
