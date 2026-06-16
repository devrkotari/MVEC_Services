namespace Mvec.Vendor.Api.Domain;

/// <summary>A KYC document uploaded by a vendor (vnd.VendorKycDocuments). Child of the Vendor aggregate.</summary>
public class VendorKycDocument
{
    public long Id { get; private set; }
    public long VendorId { get; private set; }
    public KycDocType DocType { get; private set; }
    public string BlobUrl { get; private set; } = string.Empty;
    public KycDocStatus Status { get; private set; } = KycDocStatus.Submitted;
    public DateTime UploadedUtc { get; private set; } = DateTime.UtcNow;

    private VendorKycDocument() { }

    public static VendorKycDocument Create(long vendorId, KycDocType docType, string blobUrl) => new()
    {
        VendorId = vendorId,
        DocType = docType,
        BlobUrl = blobUrl,
        Status = KycDocStatus.Submitted
    };

    /// <summary>Re-uploads a document of the same type: swap the blob and reset to Submitted.</summary>
    public void Replace(string blobUrl)
    {
        BlobUrl = blobUrl;
        Status = KycDocStatus.Submitted;
        UploadedUtc = DateTime.UtcNow;
    }

    public void MarkApproved() => Status = KycDocStatus.Approved;
    public void MarkRejected() => Status = KycDocStatus.Rejected;
}
