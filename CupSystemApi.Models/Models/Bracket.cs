using System.Text.RegularExpressions;

namespace CupSystemApi.Models;

public class Bracket
{
    public int Id { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = default!;

    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
