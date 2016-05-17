using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;
using MTDB.Core.ViewModels.Lineups;

namespace MTDB.Core.Services
{
    public class LineupService
    {
        private MtdbRepository _repository;

        public LineupService(MtdbRepository repository)
        {
            _repository = repository;
        }

        public LineupService() : this(new MtdbRepository())
        { }

        public async Task<IEnumerable<LineupSearchPlayerDto>> GetLineupPlayers(CancellationToken token)
        {
            return await Task.Run(() =>
            _repository.Players
            .OrderBy(p => p.Name)
            .Select(p => new LineupSearchPlayerDto() { Name = p.Name + " - OVR " + p.Overall, Id = p.Id }), token);
        }

        public async Task<int> UpdateLineup(ApplicationUser user, CreateLineupDto dto, CancellationToken token)
        {
            var existingLineup = await _repository.Lineups.FirstOrDefaultAsync(x => x.Id == dto.Id, token);

            if (existingLineup == null)
            {
                return -1;
            }

            if (existingLineup.User.Id != user.Id)
            {
                return -1;
            }

            existingLineup.Name = dto.Name;

            await UpdatePosition(existingLineup, LineupPosition.Bench1, dto.Bench1Id);
            await UpdatePosition(existingLineup, LineupPosition.Bench2, dto.Bench2Id);
            await UpdatePosition(existingLineup, LineupPosition.Bench3, dto.Bench3Id);
            await UpdatePosition(existingLineup, LineupPosition.Bench4, dto.Bench4Id);
            await UpdatePosition(existingLineup, LineupPosition.Bench5, dto.Bench5Id);
            await UpdatePosition(existingLineup, LineupPosition.Bench6, dto.Bench6Id);
            await UpdatePosition(existingLineup, LineupPosition.Bench7, dto.Bench7Id);
            await UpdatePosition(existingLineup, LineupPosition.Bench8, dto.Bench8Id);
            await UpdatePosition(existingLineup, LineupPosition.Center, dto.CenterId);
            await UpdatePosition(existingLineup, LineupPosition.PointGuard, dto.PointGuardId);
            await UpdatePosition(existingLineup, LineupPosition.PowerForward, dto.PowerForwardId);
            await UpdatePosition(existingLineup, LineupPosition.ShootingGuard, dto.ShootingGuardId);
            await UpdatePosition(existingLineup, LineupPosition.SmallForward, dto.SmallForwardId);

            // recalculate stats
            SetLineupStats(existingLineup);

            await _repository.SaveChangesAsync(token);

            return existingLineup.Id;
        }

        private async Task UpdatePosition(Lineup lineup, LineupPosition position, int? playerId)
        {
            var existingPlayer = lineup.Players.FirstOrDefault(x => x.LineupPosition == position);
            if (existingPlayer?.Id != playerId)
            {
                await lineup.RemovePlayer(_repository, existingPlayer?.Id, position);
                await lineup.AddPlayer(_repository, playerId, position);
            }
        }

        private void SetLineupStats(Lineup lineup)
        {
            if (lineup.Players.HasItems())
            {
                lineup.Overall = (int)lineup.Players.Average(s => s.Player.Overall);
                lineup.OutsideScoring = (int)lineup.Players.Average(s => s.Player.OutsideScoring.GetValueOrDefault(0));
                lineup.InsideScoring = (int)lineup.Players.Average(s => s.Player.InsideScoring.GetValueOrDefault(0));
                lineup.Playmaking = (int)lineup.Players.Average(s => s.Player.Playmaking.GetValueOrDefault(0));
                lineup.Athleticism = (int)lineup.Players.Average(s => s.Player.Athleticism.GetValueOrDefault(0));
                lineup.Defending = (int)lineup.Players.Average(s => s.Player.Defending.GetValueOrDefault(0));
                lineup.Rebounding = (int)lineup.Players.Average(s => s.Player.Rebounding.GetValueOrDefault(0));
                lineup.Xbox = lineup.Players.Sum(s => s.Player.Xbox.GetValueOrDefault(0));
                lineup.PS4 = lineup.Players.Sum(s => s.Player.PS4.GetValueOrDefault(0));
                lineup.PC = lineup.Players.Sum(s => s.Player.PC.GetValueOrDefault(0));
                lineup.PlayerCount = lineup.Players.Count;
            }
            else
            {
                lineup.Overall = 0;
                lineup.OutsideScoring = 0;
                lineup.InsideScoring = 0;
                lineup.Playmaking = 0;
                lineup.Athleticism = 0;
                lineup.Defending = 0;
                lineup.Rebounding = 0;
                lineup.Xbox = 0;
                lineup.PS4 = 0;
                lineup.PC = 0;
                lineup.PlayerCount = 0;
            }
        }

        public async Task<int> CreateLineup(ApplicationUser user, CreateLineupDto dto, CancellationToken token)
        {
            var lineup = new Lineup
            {
                Name = dto.Name.ReplaceBlockedWordsWithMTDB(),
                User = user
            };

            await lineup.AddPlayer(_repository, dto.PointGuardId, LineupPosition.PointGuard);
            await lineup.AddPlayer(_repository, dto.ShootingGuardId, LineupPosition.ShootingGuard);
            await lineup.AddPlayer(_repository, dto.SmallForwardId, LineupPosition.SmallForward);
            await lineup.AddPlayer(_repository, dto.PowerForwardId, LineupPosition.PowerForward);
            await lineup.AddPlayer(_repository, dto.CenterId, LineupPosition.Center);
            await lineup.AddPlayer(_repository, dto.Bench1Id, LineupPosition.Bench1);
            await lineup.AddPlayer(_repository, dto.Bench2Id, LineupPosition.Bench2);
            await lineup.AddPlayer(_repository, dto.Bench3Id, LineupPosition.Bench3);
            await lineup.AddPlayer(_repository, dto.Bench4Id, LineupPosition.Bench4);
            await lineup.AddPlayer(_repository, dto.Bench5Id, LineupPosition.Bench5);
            await lineup.AddPlayer(_repository, dto.Bench6Id, LineupPosition.Bench6);
            await lineup.AddPlayer(_repository, dto.Bench7Id, LineupPosition.Bench7);
            await lineup.AddPlayer(_repository, dto.Bench8Id, LineupPosition.Bench8);

            SetLineupStats(lineup);

            _repository.Lineups.Add(lineup);


            await _repository.SaveChangesAsync(token);

            return lineup.Id;
        }

        public async Task<LineupDto> GetLineup(int id, CancellationToken cancellationToken)
        {
            var lineup = await _repository.LineupsWithPlayers
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

            return lineup.ToDto();
        }

        public async Task DeleteLineup(int id, CancellationToken cancellationToken)
        {
            var lineup = await _repository.Lineups
                .Include(x => x.Players)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

            if (lineup != null)
            {
                _repository.LineupPlayers.RemoveRange(lineup.Players);
                _repository.Lineups.Remove(lineup);

                await _repository.SaveChangesAsync(cancellationToken); ;
            }
        }

        public async Task<LineupSearchViewModel> SearchLineups(int skip, int take, string sortByColumn, SortOrder sortOrder, CancellationToken cancellationToken)
        {
            var lineups = _repository.LineupsWithPlayers;

            var map = new Dictionary<string, string>
            {
                {"Author", "User.UserName"},
                {"DateAdded", "CreatedDate" },
                {"Title", "Name" },

            };

            return new LineupSearchViewModel()
            {
                RecordCount = await lineups.CountAsync(cancellationToken),
                Records = await lineups.Sort(sortByColumn, sortOrder, "CreatedDate", skip, take, map).ToSearchDtos(cancellationToken)
            };
        }

        public async Task<LineupPlayerDto> GetLineupPlayer(int lineupId, int playerId, LineupPosition position, CancellationToken token)
        {
            var player = await _repository.LineupPlayers
                .SingleOrDefaultAsync(lp => lp.Id == lineupId && lp.Player.Id == playerId && lp.LineupPosition == position, token);

            return player.ToLineupPlayerDto();
        }
    }

    public class LineupSearchViewModel
    {
        public int RecordCount { get; set; }
        public IEnumerable<LineupSearchDto> Records { get; set; }
    }
}

