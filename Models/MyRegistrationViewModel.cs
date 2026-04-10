namespace EventManagementPortal.Models;

public class MyRegistrationViewModel
{
    public int RegistrationID { get; set; }
    public int CompetitionID { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public DateTime CompetitionStartDate { get; set; }
    public string CompetitionLocation { get; set; } = string.Empty;

    public string RegistrationType { get; set; } = RegistrationTypes.Individual;

    /// <summary>Raw <c>registration.status</c> from database.</summary>
    public string RegistrationStatus { get; set; } = RegistrationStatuses.Pending;

    /// <summary>User-facing summary (includes ticket issued).</summary>
    public string DisplayStatus { get; set; } = string.Empty;

    /// <summary>True when the student may upload a first payment proof (pending reg, no payment row).</summary>
    public bool CanUploadPayment { get; set; }
}
