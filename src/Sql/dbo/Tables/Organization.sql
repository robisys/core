﻿CREATE TABLE [dbo].[Organization] (
    [Id]                   UNIQUEIDENTIFIER NOT NULL,
    [Name]                 NVARCHAR (50)    NOT NULL,
    [BusinessName]         NVARCHAR (50)    NULL,
    [BillingEmail]         NVARCHAR (50)    NOT NULL,
    [Plan]                 NVARCHAR (50)    NOT NULL,
    [PlanType]             TINYINT          NOT NULL,
    [Seats]                SMALLINT         NULL,
    [MaxCollections]       SMALLINT         NULL,
    [UseGroups]            BIT              NOT NULL,
    [UseDirectory]         BIT              NOT NULL,
    [StripeCustomerId]     VARCHAR (50)     NULL,
    [StripeSubscriptionId] VARCHAR (50)     NULL,
    [Enabled]              BIT              NOT NULL,
    [CreationDate]         DATETIME2 (7)    NOT NULL,
    [RevisionDate]         DATETIME2 (7)    NOT NULL,
    CONSTRAINT [PK_Organization] PRIMARY KEY CLUSTERED ([Id] ASC)
);

