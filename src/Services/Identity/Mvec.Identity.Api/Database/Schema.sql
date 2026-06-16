/* =====================================================================
   MVEC Identity — database schema (database-first; source of truth)
   Target database: Mvec_Identity   Schema: idn
   The Identity service MAPS to these tables and does not migrate them.
   ===================================================================== */

IF SCHEMA_ID(N'idn') IS NULL
    EXEC(N'CREATE SCHEMA idn AUTHORIZATION dbo;');
GO

-- Users (buyers, vendors, admins) -------------------------------------------
IF OBJECT_ID(N'idn.Users', N'U') IS NULL
CREATE TABLE idn.Users
(
    UserId          BIGINT        IDENTITY(1,1) NOT NULL,
    Email           NVARCHAR(256)    NOT NULL,
    PasswordHash    NVARCHAR(512)    NOT NULL,          -- NULL for social-only accounts
    FirstName        NVARCHAR(150)    NOT NULL,
    LastName        NVARCHAR(150)    NOT NULL,
    PhoneNumber     NVARCHAR(32)     NULL,
    UserType        VARCHAR(25)      NOT NULL CONSTRAINT DF_Users_Type DEFAULT ('Buyer'),
    [Status]        VARCHAR(25)      NOT NULL CONSTRAINT DF_Users_Status DEFAULT ('Active'),
    EmailConfirmed  BIT              NOT NULL CONSTRAINT DF_Users_EmailConf DEFAULT (0),
    PhoneConfirmed  BIT              NOT NULL CONSTRAINT DF_Users_PhoneConf DEFAULT (0),
    TwoFactorEnabled BIT             NOT NULL CONSTRAINT DF_Users_2FA DEFAULT (0),
    TwoFactorSecret NVARCHAR(256)    NULL,          -- TOTP secret (admin 2FA)
    AccessFailedCount INT            NOT NULL CONSTRAINT DF_Users_AFC DEFAULT (0),
    LastLoginUtc    DATETIME2(3)     NULL,
    CreatedUtc      DATETIME2(3)     NOT NULL CONSTRAINT DF_Users_Created DEFAULT (SYSUTCDATETIME()),
    UpdatedUtc      DATETIME2(3)     NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Type CHECK (UserType IN ('Buyer','Vendor','Admin')),
    CONSTRAINT CK_Users_Status CHECK ([Status] IN ('Active','Suspended','Deleted')),
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%_@_%._%')
);
GO

-- External Logins -----------------------------------------------------------
IF OBJECT_ID(N'idn.ExternalLogins', N'U') IS NULL
CREATE TABLE idn.ExternalLogins
(
    ExternalLoginId BIGINT        IDENTITY(1,1) NOT NULL,
    UserId          BIGINT        NOT NULL,
    Provider        VARCHAR(20)   NOT NULL,        -- Google / Facebook
    ProviderKey     NVARCHAR(256) NOT NULL,        -- subject id from provider
    CreatedUtc      DATETIME2(3)  NOT NULL CONSTRAINT DF_ExtLogin_Created DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ExternalLogins PRIMARY KEY CLUSTERED (ExternalLoginId),
    CONSTRAINT FK_ExtLogin_Users FOREIGN KEY (UserId) REFERENCES idn.Users(UserId) ON DELETE CASCADE,
    CONSTRAINT UQ_ExtLogin UNIQUE (Provider, ProviderKey),
    CONSTRAINT CK_ExtLogin_Provider CHECK (Provider IN ('Google','Facebook'))
);
GO

-- Roles ---------------------------------------------------------------------
IF OBJECT_ID(N'idn.Roles', N'U') IS NULL
CREATE TABLE idn.Roles
(
    RoleId     INT          IDENTITY(1,1) NOT NULL,
    Name       NVARCHAR(40) NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId),
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);
GO

-- UserRoles -----------------------------------------------------------------
IF OBJECT_ID(N'idn.UserRoles', N'U') IS NULL
CREATE TABLE idn.UserRoles
(
    UserId BIGINT NOT NULL,
    RoleId INT    NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES idn.Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES idn.Roles(RoleId) ON DELETE CASCADE
);
GO

-- Refresh Tokens (JWT refresh rotation) -------------------------------------
IF OBJECT_ID(N'idn.RefreshTokens', N'U') IS NULL
CREATE TABLE idn.RefreshTokens
(
    RefreshTokenId    BIGINT           IDENTITY(1,1) NOT NULL,
    UserId            BIGINT           NOT NULL,
    TokenHash         VARBINARY(64)    NOT NULL,
    JwtId             UNIQUEIDENTIFIER NULL,
    ExpiresUtc        DATETIME2(3)     NOT NULL,
    CreatedUtc        DATETIME2(3)     NOT NULL CONSTRAINT DF_RT_Created DEFAULT (SYSUTCDATETIME()),
    RevokedUtc        DATETIME2(3)     NULL,
    IsRevoked            BIGINT           NOT NULL,
    ReplacedByToken	NVARCHAR(500)	NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (RefreshTokenId),
    CONSTRAINT FK_RT_Users FOREIGN KEY (UserId) REFERENCES idn.Users(UserId) ON DELETE CASCADE,
    CONSTRAINT CK_RT_Expiry CHECK (ExpiresUtc > CreatedUtc)
);
GO

-- User Addresses ------------------------------------------------------------
IF OBJECT_ID(N'idn.UserAddresses', N'U') IS NULL
CREATE TABLE idn.UserAddresses
(
    UserAddressId BIGINT        IDENTITY(1,1) NOT NULL,
    UserId        BIGINT        NOT NULL,
    Line1         NVARCHAR(200) NOT NULL,
    Line2         NVARCHAR(200) NULL,
    City          NVARCHAR(100) NOT NULL,
    [State]       NVARCHAR(100) NULL,
    PostalCode    NVARCHAR(20)  NOT NULL,
    CountryCode   CHAR(2)       NOT NULL CONSTRAINT DF_Addr_Country DEFAULT ('IN'),
    Phone         NVARCHAR(32)  NULL,
    IsDefault     BIT           NOT NULL CONSTRAINT DF_Addr_Default DEFAULT (0),
    CreatedUtc    DATETIME2(3)  NOT NULL CONSTRAINT DF_Addr_Created DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_UserAddresses PRIMARY KEY CLUSTERED (UserAddressId),
    CONSTRAINT FK_Addr_Users FOREIGN KEY (UserId) REFERENCES idn.Users(UserId) ON DELETE CASCADE
);
GO

-- Otp Codes (email / phone) -------------------------------------------------
IF OBJECT_ID(N'idn.OtpCodes', N'U') IS NULL
CREATE TABLE idn.OtpCodes
(
    OtpId      BIGINT        IDENTITY(1,1) NOT NULL,
    UserId     BIGINT        NULL,
    Purpose    VARCHAR(25)   NOT NULL,        -- EmailVerify / PhoneVerify / PasswordReset
    CodeHash   VARBINARY(64) NOT NULL,
    ExpiresUtc DATETIME2(3)  NOT NULL,
    ConsumedUtc DATETIME2(3) NULL,
    CreatedUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_Otp_Created DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_OtpCodes PRIMARY KEY CLUSTERED (OtpId),
    CONSTRAINT FK_Otp_Users FOREIGN KEY (UserId) REFERENCES idn.Users(UserId) ON DELETE CASCADE,
    CONSTRAINT CK_Otp_Purpose CHECK (Purpose IN ('EmailVerify','PhoneVerify','PasswordReset'))
);
GO

------------------------------------------------------------------------------
-- INDEXES
------------------------------------------------------------------------------
IF IndexProperty(OBJECT_ID(N'idn.Users'), N'IX_Users_Type', N'IndexID') IS NULL
    CREATE INDEX IX_Users_Type ON idn.Users(UserType) WHERE [Status] = 'Active';
IF IndexProperty(OBJECT_ID(N'idn.ExternalLogins'), N'IX_ExtLogin_UserId', N'IndexID') IS NULL
    CREATE INDEX IX_ExtLogin_UserId ON idn.ExternalLogins(UserId);
IF IndexProperty(OBJECT_ID(N'idn.UserRoles'), N'IX_UserRoles_RoleId', N'IndexID') IS NULL
    CREATE INDEX IX_UserRoles_RoleId ON idn.UserRoles(RoleId);
IF IndexProperty(OBJECT_ID(N'idn.RefreshTokens'), N'IX_RT_UserId', N'IndexID') IS NULL
    CREATE INDEX IX_RT_UserId ON idn.RefreshTokens(UserId, ExpiresUtc);
IF IndexProperty(OBJECT_ID(N'idn.UserAddresses'), N'IX_Addr_UserId', N'IndexID') IS NULL
    CREATE INDEX IX_Addr_UserId ON idn.UserAddresses(UserId);
IF IndexProperty(OBJECT_ID(N'idn.OtpCodes'), N'IX_Otp_UserId', N'IndexID') IS NULL
    CREATE INDEX IX_Otp_UserId ON idn.OtpCodes(UserId, Purpose);
GO

PRINT 'MVEC_Identity schema created.';
GO
