namespace EventManagementPortal.Models;

/// <summary>Values aligned with CHECK constraints on <c>registration.status</c>.</summary>
public static class RegistrationStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

/// <summary>Values aligned with CHECK constraints on <c>payment.status</c>.</summary>
public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

/// <summary>Values aligned with CHECK constraints on <c>registration.type</c>.</summary>
public static class RegistrationTypes
{
    public const string Individual = "Individual";
    public const string Team = "Team";
}
