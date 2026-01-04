namespace CupSystemApi.Models;

public class TournamentParticipant
{
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = default!;

    public int UserId { get; set; }
    public User User { get; set; } = default!;
}
