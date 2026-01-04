using BCrypt.Net;
using CupSystemApi.Data;
using CupSystemApi.Models;
using CupSystemApi.Services;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CupSystemApi.GraphQL;

public class Mutation
{
    // --- AUTH ---
    public async Task<string> Register([Service] AppDbContext db, [Service] JwtService jwt, RegisterInput input)
    {
        var exists = await db.Users.AnyAsync(u => u.Email == input.Email);
        if (exists) throw new GraphQLException("Email już istnieje.");

        var user = new User
        {
            FirstName = input.FirstName,
            LastName = input.LastName,
            Email = input.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return jwt.CreateToken(user);
    }

    public async Task<string> Login([Service] AppDbContext db, [Service] JwtService jwt, LoginInput input)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == input.Email);
        if (user == null) throw new GraphQLException("Błędny email lub hasło.");

        var ok = BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash);
        if (!ok) throw new GraphQLException("Błędny email lub hasło.");

        return jwt.CreateToken(user);
    }

    // --- TOURNAMENT ---
    public async Task<Tournament> CreateTournament([Service] AppDbContext db, string name, DateTime startDate)
    {
        var t = new Tournament { Name = name, StartDate = startDate, Status = "Draft" };
        db.Tournaments.Add(t);
        await db.SaveChangesAsync();
        return t;
    }

    // Tournament.addParticipant(user)
    public async Task<Tournament> AddParticipant([Service] AppDbContext db, int tournamentId, int userId)
    {
        var t = await db.Tournaments
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t == null) throw new GraphQLException("Turniej nie istnieje.");

        var u = await db.Users.FindAsync(userId);
        if (u == null) throw new GraphQLException("Użytkownik nie istnieje.");

        var already = t.Participants.Any(p => p.UserId == userId);
        if (!already)
        {
            t.Participants.Add(new TournamentParticipant { TournamentId = t.Id, UserId = u.Id });
            await db.SaveChangesAsync();
        }

        return t;
    }

    // Tournament.start()
    public async Task<Tournament> StartTournament([Service] AppDbContext db, int tournamentId)
    {
        var t = await db.Tournaments.FindAsync(tournamentId);
        if (t == null) throw new GraphQLException("Turniej nie istnieje.");
        t.Status = "Started";
        await db.SaveChangesAsync();
        return t;
    }

    // Tournament.finish()
    public async Task<Tournament> FinishTournament([Service] AppDbContext db, int tournamentId)
    {
        var t = await db.Tournaments.FindAsync(tournamentId);
        if (t == null) throw new GraphQLException("Turniej nie istnieje.");
        t.Status = "Finished";
        await db.SaveChangesAsync();
        return t;
    }

    // --- BRACKET ---
    // Bracket.generateBracket(participants)
    public async Task<Bracket> GenerateBracket([Service] AppDbContext db, int tournamentId)
    {
        var t = await db.Tournaments
            .Include(x => x.Participants)
            .ThenInclude(tp => tp.User)
            .Include(x => x.Bracket)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t == null) throw new GraphQLException("Turniej nie istnieje.");
        if (t.Participants.Count < 2) throw new GraphQLException("Za mało uczestników.");
        if (t.Bracket != null) throw new GraphQLException("Drabinka już istnieje.");

        var users = t.Participants.Select(p => p.User).ToList();

        var bracket = new Bracket { TournamentId = t.Id };
        db.Brackets.Add(bracket);
        await db.SaveChangesAsync();

        // Minimalny generator: paruje po kolei w rundzie 1
        var pairs = users.Count / 2;

        for (int i = 0; i < pairs; i++)
        {
            var m = new Match
            {
                BracketId = bracket.Id,
                Round = 1,
                Player1Id = users[2 * i].Id,
                Player2Id = users[2 * i + 1].Id
            };
            db.Matches.Add(m);
        }

        await db.SaveChangesAsync();

        return await db.Brackets
            .Include(b => b.Matches)
            .ThenInclude(m => m.Player1)
            .Include(b => b.Matches)
            .ThenInclude(m => m.Player2)
            .FirstAsync(b => b.Id == bracket.Id);
    }

    // Bracket.getMatchesForRound(round) jako MUTACJA (zgodnie z poleceniem)
    public async Task<List<Match>> GetMatchesForRound([Service] AppDbContext db, int bracketId, int round)
    {
        return await db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.BracketId == bracketId && m.Round == round)
            .AsNoTracking()
            .ToListAsync();
    }

    // --- MATCH ---
    // Match.play(winner)
    public async Task<Match> PlayMatch([Service] AppDbContext db, int matchId, int winnerId)
    {
        var match = await db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null) throw new GraphQLException("Mecz nie istnieje.");

        if (winnerId != match.Player1Id && winnerId != match.Player2Id)
            throw new GraphQLException("Zwycięzca musi być Player1 lub Player2.");

        match.WinnerId = winnerId;
        await db.SaveChangesAsync();

        return match;
    }
}
