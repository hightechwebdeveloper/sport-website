using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
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

        public async Task<IEnumerable<LineupSearchPlayerDto>> GetLineupPlayers(CancellationToken token, bool showHidden = false)
        {
            var query = _repository.Players.AsQueryable();

            if (!showHidden)
                query = query.Where(p => !p.Private);

            query = query.OrderBy(p => p.Name);

            var players =
                await query.Select(p => new LineupSearchPlayerDto {Name = p.Name + " - OVR " + p.Overall, Id = p.Id})
                    .ToListAsync(token);
            
            return players;
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

            await UpdatePosition(existingLineup, LineupPositionType.Bench1, dto.Bench1Id);
            await UpdatePosition(existingLineup, LineupPositionType.Bench2, dto.Bench2Id);
            await UpdatePosition(existingLineup, LineupPositionType.Bench3, dto.Bench3Id);
            await UpdatePosition(existingLineup, LineupPositionType.Bench4, dto.Bench4Id);
            await UpdatePosition(existingLineup, LineupPositionType.Bench5, dto.Bench5Id);
            await UpdatePosition(existingLineup, LineupPositionType.Bench6, dto.Bench6Id);
            await UpdatePosition(existingLineup, LineupPositionType.Bench7, dto.Bench7Id);
            await UpdatePosition(existingLineup, LineupPositionType.Bench8, dto.Bench8Id);
            await UpdatePosition(existingLineup, LineupPositionType.Center, dto.CenterId);
            await UpdatePosition(existingLineup, LineupPositionType.PointGuard, dto.PointGuardId);
            await UpdatePosition(existingLineup, LineupPositionType.PowerForward, dto.PowerForwardId);
            await UpdatePosition(existingLineup, LineupPositionType.ShootingGuard, dto.ShootingGuardId);
            await UpdatePosition(existingLineup, LineupPositionType.SmallForward, dto.SmallForwardId);

            // recalculate stats
            SetLineupStats(existingLineup);

            await _repository.SaveChangesAsync(token);

            return existingLineup.Id;
        }

        private async Task UpdatePosition(Lineup lineup, LineupPositionType position, int? playerId)
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

            await lineup.AddPlayer(_repository, dto.PointGuardId, LineupPositionType.PointGuard);
            await lineup.AddPlayer(_repository, dto.ShootingGuardId, LineupPositionType.ShootingGuard);
            await lineup.AddPlayer(_repository, dto.SmallForwardId, LineupPositionType.SmallForward);
            await lineup.AddPlayer(_repository, dto.PowerForwardId, LineupPositionType.PowerForward);
            await lineup.AddPlayer(_repository, dto.CenterId, LineupPositionType.Center);
            await lineup.AddPlayer(_repository, dto.Bench1Id, LineupPositionType.Bench1);
            await lineup.AddPlayer(_repository, dto.Bench2Id, LineupPositionType.Bench2);
            await lineup.AddPlayer(_repository, dto.Bench3Id, LineupPositionType.Bench3);
            await lineup.AddPlayer(_repository, dto.Bench4Id, LineupPositionType.Bench4);
            await lineup.AddPlayer(_repository, dto.Bench5Id, LineupPositionType.Bench5);
            await lineup.AddPlayer(_repository, dto.Bench6Id, LineupPositionType.Bench6);
            await lineup.AddPlayer(_repository, dto.Bench7Id, LineupPositionType.Bench7);
            await lineup.AddPlayer(_repository, dto.Bench8Id, LineupPositionType.Bench8);

            SetLineupStats(lineup);

            _repository.Lineups.Add(lineup);


            await _repository.SaveChangesAsync(token);

            return lineup.Id;
        }

        public async Task<LineupDto> GetLineup(int id, CancellationToken cancellationToken)
        {
            var lineup = await _repository.Lineups
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

            return lineup.ToDto();
        }

        public async Task DeleteLineup(int id, CancellationToken cancellationToken)
        {
            var lineup = await _repository.Lineups
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
            var map = new Dictionary<string, string>
            {
                {"Author", "User.UserName"},
                {"DateAdded", "CreatedDate" },
                {"Title", "Name" }
            };

            var lineups = await _repository.Lineups
                .Sort(sortByColumn, sortOrder, "CreatedDate", skip, take, map)
                .ToListAsync(cancellationToken);

            return new LineupSearchViewModel
            {
                RecordCount = await _repository.Lineups.CountAsync(cancellationToken),
                Records = lineups.ToSearchDtos()
            };
        }

        public async Task<LineupPlayerDto> GetLineupPlayer(int lineupId, int playerId, LineupPositionType position, CancellationToken token)
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

