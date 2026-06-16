namespace Mvec.Vendor.Api.Domain;

/// <summary>KYC pipeline state, stored in vnd.Vendors.KycStatus (VARCHAR).</summary>
public enum KycStatus
{
    Pending = 0,
    UnderReview = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>Vendor lifecycle status, stored in vnd.Vendors.Status (VARCHAR).</summary>
public enum VendorStatus
{
    Pending = 0,
    Active = 1,
    Suspended = 2,
    Closed = 3
}

/// <summary>Commercial tier, stored in vnd.Vendors.Tier (VARCHAR). Drives BR-005 product limit.</summary>
public enum VendorTier
{
    Free = 0,
    Premium = 1
}

/// <summary>KYC document category, stored in vnd.VendorKycDocuments.DocType (VARCHAR).</summary>
public enum KycDocType
{
    GST = 0,
    NationalId = 1,
    Bank = 2,
    AddressProof = 3
}

/// <summary>Per-document review state, stored in vnd.VendorKycDocuments.Status (VARCHAR).</summary>
public enum KycDocStatus
{
    Submitted = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>Admin KYC verdict, stored in vnd.KycReviewLog.Decision (VARCHAR).</summary>
public enum KycDecision
{
    Approved = 0,
    Rejected = 1
}
