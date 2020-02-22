Use [RandomCode]

GO


Create Table [Promotion]
(
    [ID] int PRIMARY KEY IDENTITY (1,1),
    [PromotionName] VARCHAR(50) NOT NULL,
    [CodeIDStart] int UNIQUE NOT NULL,
    [CodeIDEnd] AS [CodeIDStart] + [PromotionSize] -1,
    [PromotionSize] int NOT NULL,
    [DateActive] DateTime NOT NULL,
    [DateExpires] DateTime NOT NULL
);

