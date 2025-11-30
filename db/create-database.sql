CREATE TABLE [Providers] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Imap_Host] nvarchar(max) NOT NULL,
    [Imap_Port] int NOT NULL,
    [Imap_UseSsl] bit NOT NULL,
    [Imap_TrustCertificates] bit NOT NULL,
    [Imap_Username] nvarchar(max) NULL,
    [Imap_Password] nvarchar(max) NULL,
    [Smtp_Host] nvarchar(max) NOT NULL,
    [Smtp_Port] int NOT NULL,
    [Smtp_UseTls] bit NOT NULL,
    [Smtp_TrustCertificates] bit NOT NULL,
    [Smtp_Username] nvarchar(max) NULL,
    [Smtp_Password] nvarchar(max) NULL,
    CONSTRAINT [PK_Providers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Accounts] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(max) NOT NULL,
    [ProviderId] uniqueidentifier NOT NULL,
    [EmailAddress] nvarchar(450) NOT NULL,
    [RefreshToken] nvarchar(max) NOT NULL,
    [AccessToken] nvarchar(max) NULL,
    [ExpiresAt] datetimeoffset NULL,
    [Scopes] nvarchar(max) NOT NULL,
    [DisplayName] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Accounts_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Domain] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [ProviderId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Domain] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Domain_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([Id]) ON DELETE CASCADE
);
GO


CREATE UNIQUE INDEX [IX_Accounts_EmailAddress] ON [Accounts] ([EmailAddress]);
GO


CREATE INDEX [IX_Accounts_ProviderId] ON [Accounts] ([ProviderId]);
GO


CREATE UNIQUE INDEX [IX_Domain_Name] ON [Domain] ([Name]);
GO


CREATE INDEX [IX_Domain_ProviderId] ON [Domain] ([ProviderId]);
GO


CREATE UNIQUE INDEX [IX_Providers_Name] ON [Providers] ([Name]);
GO


