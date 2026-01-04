namespace CupSystemApi.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = new List<TournamentParticipant>();
}
