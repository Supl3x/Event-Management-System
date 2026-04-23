using System.ComponentModel.DataAnnotations;

namespace EventManagementPortal.Models;

public class IndividualCompetitionRegistrationViewModel
{
    public int CompetitionID { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public decimal EntryFee { get; set; }

    [Display(Name = "I confirm I want to register for this competition")]
    public bool ConfirmRegistration { get; set; }
}
