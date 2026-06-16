/* =====================================================================
   MVEC Vendor — database schema (database-first; source of truth)
   Target database: Mvec_Vendor   Schema: vnd
   The Vendor service MAPS to these tables and does not migrate them.
   This script reflects the existing vnd.* schema; it is idempotent.
   ===================================================================== */

IF SCHEMA_ID(N'vnd') IS NULL
    EXEC(N'CREATE SCHEMA vnd AUTHORIZATION dbo;');
GO

-- Vendors (aggregate root) --------------------------------------------------
IF OBJECT_ID(N'vnd.Vendors', N'U') IS NULL
CREATE TABLE vnd.Vendors
(
    VendorId        BIGINT        IDENTITY(1,1) NOT NULL,
    OwnerUserId     BIGINT        NOT NULL,          -- idn.Users.UserId (one vendor per user)
    BusinessName    NVARCHAR(200) NOT NULL,
    BusinessType    NVARCHAR(50)  NULL,
    ContactEmail    NVARCHAR(256) NOT NULL,
    ContactPhone    NVARCHAR(32)  NULL,
    PAN             VARCHAR(15)   NULL,
    GSTIN           VARCHAR(20)   NULL,
    KycStatus       VARCHAR(15)   NOT NULL CONSTRAINT DF_Vendors_Kyc DEFAULT ('Pending'),
    Tier            VARCHAR(10)   NOT NULL CONSTRAINT DF_Vendors_Tier DEFAULT ('Free'),
    ProductLimit    INT           NOT NULL CONSTRAINT DF_Vendors_PLimit DEFAULT (50),
    CommissionPct   DECIMAL(5,2)  NOT NULL CONSTRAINT DF_Vendors_Comm DEFAULT (10.00),
    [Status]        VARCHAR(15)   NOT NULL CONSTRAINT DF_Vendors_Status DEFAULT ('Pending'),
    RatingAvg       DECIMAL(3,2)  NOT NULL CONSTRAINT DF_Vendors_Rating DEFAULT (0),
    FulfilledOrders INT           NOT NULL CONSTRAINT DF_Vendors_Orders DEFAULT (0),
    MemberSinceUtc  DATETIME2(3)  NOT NULL CONSTRAINT DF_Vendors_Member DEFAULT (SYSUTCDATETIME()),
    UpdatedUtc      DATETIME2(3)  NULL,
    CONSTRAINT PK_Vendors PRIMARY KEY CLUSTERED (VendorId),
    CONSTRAINT UQ_Vendors_Owner UNIQUE (OwnerUserId),
    CONSTRAINT CK_Vendors_Kyc CHECK (KycStatus IN ('Pending','UnderReview','Approved','Rejected')),
    CONSTRAINT CK_Vendors_Tier CHECK (Tier IN ('Free','Premium')),
    CONSTRAINT CK_Vendors_Status CHECK ([Status] IN ('Pending','Active','Suspended','Closed')),
    CONSTRAINT CK_Vendors_Comm CHECK (CommissionPct >= 0 AND CommissionPct <= 100),
    CONSTRAINT CK_Vendors_Rating CHECK (RatingAvg >= 0 AND RatingAvg <= 5),
    CONSTRAINT CK_Vendors_PLimit CHECK (ProductLimit >= 0)
);
GO

-- KYC documents -------------------------------------------------------------
IF OBJECT_ID(N'vnd.VendorKycDocuments', N'U') IS NULL
CREATE TABLE vnd.VendorKycDocuments
(
    DocumentId  BIGINT        IDENTITY(1,1) NOT NULL,
    VendorId    BIGINT        NOT NULL,
    DocType     VARCHAR(20)   NOT NULL,
    BlobUrl     NVARCHAR(500) NOT NULL,
    [Status]    VARCHAR(15)   NOT NULL CONSTRAINT DF_Kyc_Status DEFAULT ('Submitted'),
    UploadedUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_Kyc_Uploaded DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_VendorKycDocuments PRIMARY KEY CLUSTERED (DocumentId),
    CONSTRAINT FK_Kyc_Vendors FOREIGN KEY (VendorId) REFERENCES vnd.Vendors(VendorId) ON DELETE CASCADE,
    CONSTRAINT CK_Kyc_DocType CHECK (DocType IN ('GST','NationalId','Bank','AddressProof')),
    CONSTRAINT CK_Kyc_Status CHECK ([Status] IN ('Submitted','Approved','Rejected'))
);
GO

-- KYC review log (admin decisions) ------------------------------------------
IF OBJECT_ID(N'vnd.KycReviewLog', N'U') IS NULL
CREATE TABLE vnd.KycReviewLog
(
    ReviewLogId BIGINT        IDENTITY(1,1) NOT NULL,
    VendorId    BIGINT        NOT NULL,
    Decision    VARCHAR(10)   NOT NULL,
    Reason      NVARCHAR(500) NULL,
    ReviewedBy  BIGINT        NOT NULL,          -- admin idn.Users.UserId
    ReviewedUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_KycLog_Reviewed DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_KycReviewLog PRIMARY KEY CLUSTERED (ReviewLogId),
    CONSTRAINT FK_KycLog_Vendors FOREIGN KEY (VendorId) REFERENCES vnd.Vendors(VendorId) ON DELETE CASCADE,
    CONSTRAINT CK_KycLog_Decision CHECK (Decision IN ('Approved','Rejected'))
);
GO

-- Payout bank accounts (account number stored encrypted) --------------------
IF OBJECT_ID(N'vnd.VendorBankAccounts', N'U') IS NULL
CREATE TABLE vnd.VendorBankAccounts
(
    BankAccountId    BIGINT         IDENTITY(1,1) NOT NULL,
    VendorId         BIGINT         NOT NULL,
    AccountHolder    NVARCHAR(150)  NOT NULL,
    BankName         NVARCHAR(150)  NOT NULL,
    AccountNumberEnc VARBINARY(256) NOT NULL,
    IFSC             VARCHAR(15)    NULL,
    IsPrimary        BIT            NOT NULL CONSTRAINT DF_Bank_Primary DEFAULT (1),
    CreatedUtc       DATETIME2(3)   NOT NULL CONSTRAINT DF_Bank_Created DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_VendorBankAccounts PRIMARY KEY CLUSTERED (BankAccountId),
    CONSTRAINT FK_Bank_Vendors FOREIGN KEY (VendorId) REFERENCES vnd.Vendors(VendorId) ON DELETE CASCADE
);
GO

-- Stores (one per vendor) ---------------------------------------------------
IF OBJECT_ID(N'vnd.VendorStores', N'U') IS NULL
CREATE TABLE vnd.VendorStores
(
    StoreId      BIGINT         IDENTITY(1,1) NOT NULL,
    VendorId     BIGINT         NOT NULL,
    StoreName    NVARCHAR(150)  NOT NULL,
    Slug         NVARCHAR(160)  NOT NULL,
    LogoUrl      NVARCHAR(500)  NULL,
    BannerUrl    NVARCHAR(500)  NULL,
    [Description] NVARCHAR(MAX) NULL,
    ReturnPolicy NVARCHAR(MAX)  NULL,
    SocialLinks  NVARCHAR(MAX)  NULL,
    IsLive       BIT            NOT NULL CONSTRAINT DF_Store_Live DEFAULT (0),
    CreatedUtc   DATETIME2(3)   NOT NULL CONSTRAINT DF_Store_Created DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_VendorStores PRIMARY KEY CLUSTERED (StoreId),
    CONSTRAINT FK_Store_Vendors FOREIGN KEY (VendorId) REFERENCES vnd.Vendors(VendorId) ON DELETE CASCADE,
    CONSTRAINT UQ_Store_Vendor UNIQUE (VendorId),
    CONSTRAINT UQ_Store_Slug UNIQUE (Slug)
);
GO

-- Shipping zones (per store) ------------------------------------------------
IF OBJECT_ID(N'vnd.VendorShippingZones', N'U') IS NULL
CREATE TABLE vnd.VendorShippingZones
(
    ShippingZoneId BIGINT        IDENTITY(1,1) NOT NULL,
    StoreId        BIGINT        NOT NULL,
    ZoneName       NVARCHAR(100) NOT NULL,
    Regions        NVARCHAR(MAX) NULL,
    FlatRate       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Zone_Rate DEFAULT (0),
    FreeAbove      DECIMAL(18,2) NULL,
    CONSTRAINT PK_VendorShippingZones PRIMARY KEY CLUSTERED (ShippingZoneId),
    CONSTRAINT FK_Zone_Stores FOREIGN KEY (StoreId) REFERENCES vnd.VendorStores(StoreId) ON DELETE CASCADE,
    CONSTRAINT CK_Zone_Rate CHECK (FlatRate >= 0)
);
GO

------------------------------------------------------------------------------
-- INDEXES
------------------------------------------------------------------------------
IF IndexProperty(OBJECT_ID(N'vnd.Vendors'), N'IX_Vendors_Status', N'IndexID') IS NULL
    CREATE INDEX IX_Vendors_Status ON vnd.Vendors([Status]);
IF IndexProperty(OBJECT_ID(N'vnd.Vendors'), N'IX_Vendors_KycStatus', N'IndexID') IS NULL
    CREATE INDEX IX_Vendors_KycStatus ON vnd.Vendors(BusinessName, [Status], KycStatus);
IF IndexProperty(OBJECT_ID(N'vnd.VendorKycDocuments'), N'IX_Kyc_VendorId', N'IndexID') IS NULL
    CREATE INDEX IX_Kyc_VendorId ON vnd.VendorKycDocuments(VendorId, [Status]);
IF IndexProperty(OBJECT_ID(N'vnd.KycReviewLog'), N'IX_KycLog_VendorId', N'IndexID') IS NULL
    CREATE INDEX IX_KycLog_VendorId ON vnd.KycReviewLog(VendorId, ReviewedUtc);
IF IndexProperty(OBJECT_ID(N'vnd.VendorBankAccounts'), N'IX_Bank_VendorId', N'IndexID') IS NULL
    CREATE INDEX IX_Bank_VendorId ON vnd.VendorBankAccounts(VendorId);
IF IndexProperty(OBJECT_ID(N'vnd.VendorShippingZones'), N'IX_Zone_StoreId', N'IndexID') IS NULL
    CREATE INDEX IX_Zone_StoreId ON vnd.VendorShippingZones(StoreId);
GO

PRINT 'MVEC_Vendor schema verified.';
GO
