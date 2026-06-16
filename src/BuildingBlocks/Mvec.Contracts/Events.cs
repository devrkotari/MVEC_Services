namespace Mvec.Contracts.Events;

// Identity
public record UserRegistered(long UserId, string Email, string Role, DateTime OccurredAt);

// Vendor (keys are BIGINT IDENTITY in vnd.* — long, consistent with UserRegistered)
public record VendorRegistered(long VendorId, long OwnerUserId, string BusinessName, DateTime OccurredAt);
public record VendorApproved(long VendorId, long OwnerUserId, DateTime OccurredAt);
public record VendorRejected(long VendorId, long OwnerUserId, string Reason, DateTime OccurredAt);

// Order
public record OrderLine(Guid ProductId, string Name, decimal UnitPrice, int Qty);
public record OrderPlaced(Guid OrderId, Guid BuyerId, Guid VendorId, decimal Total, IReadOnlyList<OrderLine> Lines, DateTime OccurredAt);
public record OrderShipped(Guid OrderId, Guid BuyerId, string Carrier, string TrackingNo);
public record OrderCancelled(Guid OrderId, Guid BuyerId, string Reason);
public record OrderDelivered(Guid OrderId, Guid VendorId, decimal Total);

// Payment
public record PaymentCaptured(Guid OrderId, Guid PaymentId, decimal Amount);
public record PaymentFailed(Guid OrderId, string Reason);
public record PaymentRefunded(Guid OrderId, Guid RefundId, decimal Amount);
public record PayoutReleased(Guid VendorId, Guid PayoutId, decimal NetAmount);

// Review
public record ReviewSubmitted(Guid ProductId, Guid VendorId, int Rating);
