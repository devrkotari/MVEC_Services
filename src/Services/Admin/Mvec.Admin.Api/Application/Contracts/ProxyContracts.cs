namespace Mvec.Admin.Api.Application.Contracts;

/// <summary>Body for rejecting a vendor (forwarded to the Vendor service; reason is required).</summary>
public sealed record RejectVendorRequest(string Reason);

/// <summary>Body for changing a user's account status (forwarded to the Identity service).</summary>
public sealed record ChangeUserStatusRequest(string Status);
