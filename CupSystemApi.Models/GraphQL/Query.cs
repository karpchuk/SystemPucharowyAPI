using CupSystemApi.Data;
using CupSystemApi.Models;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CupSystemApi.GraphQL;

public class Query
{
    public IQueryable<Tournament> Tournaments([Service] AppDbContext db)
        => db.Tournaments.AsNoTracking();

    public IQueryable<Match> Matches([Service] AppDbContext db)
        => db.Matches.AsNoTracking();

    //użytkownik pobiera swoje mecze bez podawania id
    [Authorize]
    public async Task<List<Match>> MyMatches(
        [Service] AppDbContext db,
        ClaimsPrincipal me)
    {
        var userIdStr = me.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return new List<Match>();

        return await db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.Player1Id == userId || m.Player2Id == userId)
            .AsNoTracking()
            .ToListAsync();
    }
}
