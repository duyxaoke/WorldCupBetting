﻿using DFDSBetting.Models;
using System.Threading.Tasks;
using System.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DFDSBetting.Services
{
    public class BetService
    {
        ApplicationDbContext _context;
        UserService _userService;
        MatchService _matchService;
        TeamService _teamService;

        public BetService(ApplicationDbContext context)
        {
            _context = context;
            _userService = new UserService(_context);
            _matchService = new MatchService(_context);
            _teamService = new TeamService(_context);
        }

        internal async Task<List<WinnerBetIndexViewModel>> GetListOfTeamsWithBetsAsync()
        {
            var teams = await _teamService.GetAllAsync();
            var loggedInUser = await _userService.GetLoggedInUserAsync();

            var WinnerBetList = new List<WinnerBetIndexViewModel>();

            foreach (var team in teams)
            {
                WinnerBetList.Add(new WinnerBetIndexViewModel
                {
                    Team = new TeamViewModel
                    {
                        Id = team.Id,
                        Name = team.Name,
                        FlagUrl = team.FlagUrl
                    },
                    WinnerBet = await GetWinnerBetForATeamAsync(team)
                });
            }
            return WinnerBetList;
        }

        public async Task<WinnerBetViewModel> GetWinnerBetForATeamAsync(Team team)
        {
            var loggedInUser = await _userService.GetLoggedInUserAsync();
            var bet = await _context.WinnerBets.FirstOrDefaultAsync(b => b.Team.Id == team.Id && loggedInUser.Id == b.Placer.Id);

            if (bet == null)
            {
                return new WinnerBetViewModel
                {
                    Id = Guid.Empty,
                    TeamId = team.Id
                };
            }

            return new WinnerBetViewModel
            {
                Id = bet.Id,
                TeamId = team.Id
            };
        }

        public async Task MakeNewScoreBet(NewScoreBetViewModel newBet) 
        {
            var bet = new ScoreBet
            {
                Id = Guid.NewGuid(),
                Match = await _matchService.GetByIdAsync(newBet.MatchId),
                Placer = await _userService.GetLoggedInUserAsync(),
                ScoreHome = newBet.ScoreHome,
                ScoreAway = newBet.ScoreAway
            };
            _context.ScoreBets.Add(bet);

            await _context.SaveChangesAsync();
        }

        public async Task MakeNewWinnerBet(NewWinnerBetViewModel newBet)
        {
            var loggedInUser = await _userService.GetLoggedInUserAsync();
            _context.WinnerBets.RemoveRange(_context.WinnerBets.Where(b => b.Placer.Id == loggedInUser.Id));

            await _context.SaveChangesAsync();

            var bet = new WinnerBet
            {
                Id = Guid.NewGuid(),
                Team = await _teamService.GetByIdAsync(newBet.TeamId),
                Placer = await _userService.GetLoggedInUserAsync(),
            };
            _context.WinnerBets.Add(bet);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateScoreBet(ScoreBetViewModel scoreBet)
        {
            ScoreBet updatedBet = await this.GetScoreBetByIdAsync(scoreBet.Id);

            updatedBet.ScoreHome = scoreBet.ScoreHome;
            updatedBet.ScoreAway = scoreBet.ScoreAway;

            _context.SaveChanges();
        }

        public async Task UpdateWinnderBet(WinnerBetViewModel winnerBet)
        {
            WinnerBet updatedBet = await this.GetWinnerBetByIdAsync(winnerBet.Id);
            updatedBet.Team = await _teamService.GetByIdAsync(winnerBet.TeamId);

            await _context.SaveChangesAsync();
        }

        public async Task<WinnerBet> GetWinnerBetByIdAsync(Guid id)
        {
            return await _context.WinnerBets.FirstAsync(b => b.Id == id);
        }

        public async Task<ScoreBet> GetScoreBetByIdAsync(Guid id)
        {
            return await _context.ScoreBets.FirstAsync(b => b.Id == id);
        }

        public async Task<ScoreBet> GetBetForAMatchAsync(Match match)
        {
            var user = await _userService.GetLoggedInUserAsync();

            return await _context.ScoreBets.FirstOrDefaultAsync(b => b.Match.Id == match.Id && b.Placer.Id == user.Id);
        }

        public async Task<ScoreBetViewModel> GetScoreBetViewModelForAMatchAsync(Match match)
        {
            var scoreBet = await GetBetForAMatchAsync(match);

            if (scoreBet == null)
            {
                return null;
            }

            return new ScoreBetViewModel
            {
                Id = scoreBet.Id,
                ScoreAway = scoreBet.ScoreAway,
                ScoreHome = scoreBet.ScoreHome
            };
        }

        internal async Task<List<ScoreBetIndexViewModel>> GetListOfMatchesWithBetsAsync()
        {
            var matches = await _context.Matches
                .Where(m => m.AwayTeam != null && m.HomeTeam != null)
                .OrderBy(m => m.BeginDate)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .ToListAsync();

            var BetIndexList = new List<ScoreBetIndexViewModel>();
            foreach (var match in matches)
            {
                
                BetIndexList.Add(new ScoreBetIndexViewModel
                {
                    Match = _matchService.GetMatchViewModel(match),
                    ScoreBet =  await GetScoreBetViewModelForAMatchAsync(match)
                });
            }

            return BetIndexList;
        }
    }
}