using FluentAssertions;
using Mvec.Contracts.Events;
using Mvec.Vendor.Api.Application.Contracts;
using Mvec.Vendor.Api.Domain;
using Xunit;

namespace Mvec.Vendor.Tests;

public class VendorServiceTests
{
    private const long OwnerId = 1001;
    private const long AdminId = 9001;

    private static RegisterVendorRequest NewVendor(string email = "shop@example.com") =>
        new("Acme Traders", "Sole Proprietor", email, "+910000000000", "ABCDE1234F", null);

    private static async Task<long> RegisterAsync(VendorTestHarness h, long ownerId = OwnerId)
    {
        var reg = await h.Vendors.RegisterAsync(ownerId, NewVendor());
        reg.IsSuccess.Should().BeTrue();
        return reg.Value!.Id;
    }

    private static async Task SubmitAllRequiredKycAsync(VendorTestHarness h, long ownerId = OwnerId)
    {
        foreach (var type in Mvec.Vendor.Api.Domain.Vendor.RequiredKycDocs)
            await h.Vendors.UploadKycAsync(ownerId, new UploadKycRequest(type, $"https://blob/{type}.pdf"));
    }

    [Fact]
    public async Task Register_creates_pending_vendor_and_publishes_event()
    {
        using var h = new VendorTestHarness();

        var result = await h.Vendors.RegisterAsync(OwnerId, NewVendor());

        result.IsSuccess.Should().BeTrue();
        result.Value!.KycStatus.Should().Be(KycStatus.Pending);
        result.Value.Status.Should().Be(VendorStatus.Pending);
        result.Value.IsApproved.Should().BeFalse();
        h.Events.Published.OfType<VendorRegistered>().Should().ContainSingle()
            .Which.OwnerUserId.Should().Be(OwnerId);
    }

    [Fact]
    public async Task Register_twice_for_same_user_is_conflict()
    {
        using var h = new VendorTestHarness();
        await RegisterAsync(h);

        var second = await h.Vendors.RegisterAsync(OwnerId, NewVendor("other@example.com"));

        second.IsSuccess.Should().BeFalse();
        second.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task Uploading_all_required_docs_advances_to_under_review()
    {
        using var h = new VendorTestHarness();
        await RegisterAsync(h);

        await SubmitAllRequiredKycAsync(h);

        var me = await h.Vendors.GetMineAsync(OwnerId);
        me.Value!.KycStatus.Should().Be(KycStatus.UnderReview);
    }

    [Fact]
    public async Task Re_uploading_same_doc_type_replaces_rather_than_duplicates()
    {
        using var h = new VendorTestHarness();
        var vendorId = await RegisterAsync(h);

        await h.Vendors.UploadKycAsync(OwnerId, new UploadKycRequest(KycDocType.NationalId, "https://blob/v1.pdf"));
        await h.Vendors.UploadKycAsync(OwnerId, new UploadKycRequest(KycDocType.NationalId, "https://blob/v2.pdf"));

        var detail = await h.Vendors.GetDetailAsync(vendorId);
        var nationalIdDocs = detail.Value!.Documents.Where(d => d.DocType == KycDocType.NationalId).ToList();
        nationalIdDocs.Should().ContainSingle();
        nationalIdDocs[0].BlobUrl.Should().Be("https://blob/v2.pdf");
    }

    [Fact]
    public async Task Pending_queue_lists_vendor_until_approved()
    {
        using var h = new VendorTestHarness();
        var vendorId = await RegisterAsync(h);

        var pendingBefore = await h.Vendors.ListPendingAsync(new BuildingBlocks.Common.PagedRequest());
        pendingBefore.Items.Should().ContainSingle(v => v.Id == vendorId);

        await h.Vendors.ApproveAsync(vendorId, AdminId);

        var pendingAfter = await h.Vendors.ListPendingAsync(new BuildingBlocks.Common.PagedRequest());
        pendingAfter.Items.Should().NotContain(v => v.Id == vendorId);
    }

    [Fact]
    public async Task Approve_sets_active_and_publishes_event()
    {
        using var h = new VendorTestHarness();
        var vendorId = await RegisterAsync(h);
        await SubmitAllRequiredKycAsync(h);

        var result = await h.Vendors.ApproveAsync(vendorId, AdminId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.KycStatus.Should().Be(KycStatus.Approved);
        result.Value.Status.Should().Be(VendorStatus.Active);
        result.Value.IsApproved.Should().BeTrue();
        h.Events.Published.OfType<VendorApproved>().Should().ContainSingle()
            .Which.VendorId.Should().Be(vendorId);
    }

    [Fact]
    public async Task Reject_without_reason_is_validation_error()
    {
        using var h = new VendorTestHarness();
        var vendorId = await RegisterAsync(h);

        var result = await h.Vendors.RejectAsync(vendorId, AdminId, new RejectVendorRequest("   "));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("validation");
        h.Events.Published.OfType<VendorRejected>().Should().BeEmpty();
    }

    [Fact]
    public async Task Reject_with_reason_publishes_event_carrying_reason()
    {
        using var h = new VendorTestHarness();
        var vendorId = await RegisterAsync(h);

        var result = await h.Vendors.RejectAsync(vendorId, AdminId, new RejectVendorRequest("Blurry ID document"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.KycStatus.Should().Be(KycStatus.Rejected);
        var evt = h.Events.Published.OfType<VendorRejected>().Should().ContainSingle().Which;
        evt.VendorId.Should().Be(vendorId);
        evt.Reason.Should().Be("Blurry ID document");
    }

    [Fact]
    public async Task Suspend_then_reinstate_round_trips_status()
    {
        using var h = new VendorTestHarness();
        var vendorId = await RegisterAsync(h);
        await h.Vendors.ApproveAsync(vendorId, AdminId);

        var suspended = await h.Vendors.SuspendAsync(vendorId);
        suspended.IsSuccess.Should().BeTrue();
        suspended.Value!.Status.Should().Be(VendorStatus.Suspended);
        suspended.Value.IsApproved.Should().BeFalse(); // BR-001: suspended vendor cannot list

        var reinstated = await h.Vendors.ReinstateAsync(vendorId);
        reinstated.IsSuccess.Should().BeTrue();
        reinstated.Value!.Status.Should().Be(VendorStatus.Active);
        reinstated.Value.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task Suspend_non_active_vendor_is_conflict()
    {
        using var h = new VendorTestHarness();
        var vendorId = await RegisterAsync(h); // still Pending

        var result = await h.Vendors.SuspendAsync(vendorId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("conflict");
    }
}
