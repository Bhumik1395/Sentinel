namespace Sentinel.Identity.SupportEngagements;

public record StartEngagementRequest(
    Guid OrganizationId,
    Guid SupportUserId,
    string Reason,
    int DurationHours
);

public record SupportEngagement(
    Guid Id,
    Guid OrganizationId,
    Guid SupportUserId,
    string Reason,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt
);