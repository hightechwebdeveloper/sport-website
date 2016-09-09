using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services
{
    public class CollectionService
    {
        #region Fields

        private readonly MtdbContext _dbContext;

        #endregion

        #region ctor

        public CollectionService(MtdbContext dbContext)
        {
            _dbContext = dbContext;
        }

        #endregion

        #region Utilities

        private IEnumerable<CollectionViewModel> MapCollectionToViewModel(IEnumerable<Collection> collections, string groupName = null)
        {
            return collections.OrderBy(p => p.Name).Select((collection, index) => new CollectionViewModel
            {
                Name = collection.Name,
                Group = groupName ?? collection.GroupName,
                DisplayOrder = collection.DisplayOrder ?? index
            });
        }

        #endregion

        #region Methods

        public async Task<List<Collection>> GetCollections(CancellationToken token)
        {
            return await _dbContext.Collections
                .OrderBy(p => p.Name)
                .ToListAsync(token);
        }

        public async Task<CollectionsViewModel> GetGroupedCollections(CancellationToken token)
        {
            var teams = await _dbContext.Teams
                .Include(t => t.Division)
                .ToListAsync(token);

            var collections = await _dbContext.Collections
                .ToListAsync(token);

            var current = teams
                .Where(t => !t.Name.Contains("Free")).OrderBy(p => p.Division.Name).ThenBy(p => p.Name)
                .Select((team, id) => new CollectionViewModel { Name = team.Name, Group = team.Division.Name, DisplayOrder = id })
                .ToList();

            var dynamic = teams
                .Where(t => !t.Name.Contains("Free")).OrderBy(p => p.Division.Name).ThenBy(p => p.Name)
                .Select((team, id) => new CollectionViewModel { Name = team.Name, Group = team.Division.Name, DisplayOrder = id })
                .ToList();

            var other = new List<CollectionViewModel>();

            other.AddRange(MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Gems of The Game"), "Gems of The Game"));
            other.AddRange(MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Rewards"), "Rewards"));

            var collectionsViewModel = new CollectionsViewModel
            {
                Current = current,
                CurrentFreeAgents = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Current")),
                Dynamic = dynamic,
                DynamicFreeAgents = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Dynamic")),
                Historic = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Historic")),
                Other = other
            };

            return collectionsViewModel;
        }

        public async Task<CollectionDetails> GetPlayersForCollection(int skip, int take, string sortByColumn, SortOrder sortOrder, string groupName, string name, CancellationToken token, bool showHidden = false)
        {
            // So we will receive a groupName and name with dashes instead of spaces.  Remove dashes and place spaces in.  
            groupName = groupName.Replace("-", " ");
            name = name.Replace("-", " ");
            string collectionName;


            IQueryable<Player> query = _dbContext.Players
                .Include(p => p.Tier);

            // If groupName == Dynamic or Current then we just filter by theme and team
            if (groupName.EqualsAny("dynamic", "current") && !name.Contains("free"))
            {
                var team = await _dbContext.Teams.FirstOrDefaultAsync(p => p.Name == name, token);
                if (team == null)
                    return null;

                collectionName = team.Name;
                query = query.Where(p => p.Theme.Name == groupName && p.Team.Name == name);
            }
            else
            {
                var collection = await _dbContext.Collections.FirstOrDefaultAsync(p => (p.GroupName == groupName || p.ThemeName == groupName) && p.Name == name, token);

                if (collection == null)
                    return null;

                collectionName = collection.Name;
                // Not a team so just search by collection
                query = query.Where(p => p.Collection.Id == collection.Id);
            }
            if (!showHidden)
                query = query.Where(p => !p.Private);

            var count = await query.CountAsync(token);

            if (count == 0)
            {
                return new CollectionDetails { Name = collectionName, Results = new List<Player>() };
            }

            var averages =
                await query.Select(
                    p =>
                        new
                        {
                            Overall = p.Overall,
                            OutsideScoring = p.OutsideScoring,
                            InsideScoring = p.InsideScoring,
                            Playmaking = p.Playmaking,
                            Athleticism = p.Athleticism,
                            Defending = p.Defending,
                            p.Rebounding
                        })
                        .ToListAsync(token);

            var sortMap = new Dictionary<string, string>
            {
                {"CreatedDateString", "CreatedDate"},
                {"Position", "PrimaryPosition"}
            };

            var players = await query
                .Sort(sortByColumn, sortOrder, "Overall", skip, take, sortMap)
                .ToListAsync(token);
            
            var viewModel = new CollectionDetails
            {
                Name = collectionName,
                Overall = (int)averages.Average(d => d.Overall),
                OutsideScoring = (int)(averages.Average(s => s.OutsideScoring) ?? 0),
                InsideScoring = (int)(averages.Average(s => s.InsideScoring) ?? 0),
                Playmaking = (int)(averages.Average(s => s.Playmaking) ?? 0),
                Athleticism = (int)(averages.Average(s => s.Athleticism) ?? 0),
                Defending = (int)(averages.Average(s => s.Defending) ?? 0),
                Rebounding = (int)(averages.Average(s => s.Rebounding) ?? 0),
                Results = players,
                ResultCount = count,
            };

            return viewModel;
        }

        public async Task UpdateCollection(Collection collection, CancellationToken token)
        {
            var entity = await _dbContext.Collections.FirstAsync(t => t.Id == collection.Id, token);
            _dbContext.Entry(entity).CurrentValues.SetValues(collection);
            await _dbContext.SaveChangesAsync(token);
        }

        public async Task CreateCollection(Collection collection, CancellationToken token)
        {
            _dbContext.Collections.Add(collection);
            await _dbContext.SaveChangesAsync(token);
        }

        public async Task DeleteCollection(int id, CancellationToken token)
        {
            var entity = await _dbContext.Collections
                .SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Collections.Remove(entity);
            await _dbContext.SaveChangesAsync(token);
        }

        #endregion
    }

    public class CollectionDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Overall { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
        public int ResultCount { get; set; }
        public IEnumerable<Player> Results { get; set; }
    }
}
