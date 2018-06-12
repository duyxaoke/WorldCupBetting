﻿using System;
using System.Data.Entity;
using System.Threading.Tasks;
using DFDSBetting.Models;

namespace DFDSBetting.Services
{
    internal class TeamService
    {
        ApplicationDbContext _context = new ApplicationDbContext();

        public TeamService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Team> GetByIdAsync(Guid teamId)
        {
            return await _context.Teams.FirstAsync(t => t.Id == teamId);
        }
    }
}