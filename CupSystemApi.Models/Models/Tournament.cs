namespace CupSystemApi.Models;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = "Draft"; // Draft / Started / Finished

    public Bracket? Bracket { get; set; }

    public ICollection<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();
}
