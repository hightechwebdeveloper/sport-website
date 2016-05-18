using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using MTDB.Core.EntityFramework;
using MTDB.Core.Services;

namespace _2KUpdateCode
{
    class Program
    {
        static void Main(string[] args)
        {
            // read csv
            var gamePlayers = Get2KPlayers().ToList();

            var withUrls = gamePlayers.Where(p => p["URL"] != null).Select(p => new { Url = GetUrl(p), Player = p });

            var token = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions { CancellationToken = token.Token };
            var repository = new MtdbRepository();
            var playerService = new PlayerService(repository);

            var players = playerService.GetPlayersByUri(CancellationToken.None, withUrls.Select(p => p.Url).ToArray()).Result;

            foreach (var playerUri in withUrls)
            {
                // Find this player
                var player = players.FirstOrDefault(p => p.UriName == playerUri.Url);

                if (player != null)
                {
                    var idString = playerUri.Player["ID"]?.ToString();

                    int id = 0;

                    if (int.TryParse(idString, out id))
                    {
                        player.NBA2K_ID = id;
                    }
                }
            }

            repository.SaveChangesAsync().Wait();


           // var playerUpdateService = new PlayerUpdateService(repository);
            //var result = playerUpdateService.UpdatePlayersFromFile(ConfigurationManager.AppSettings["GamePlayersFile"], CancellationToken.None).Result;


            Console.WriteLine("Done");

            Console.ReadLine();
        }



        private static string GetUrl(Dictionary<string, object> dictionary)
        {
            var url = dictionary["URL"].ToString().Replace("http://mtdb.com/nba2k16/player", "").Replace("/", "");

            url = url.Trim();
            url = url.Replace("a-c-", "ac-");
            url = url.Replace("a-j-", "aj-");
            url = url.Replace("j-j-", "jj-");
            url = url.Replace("c-j-", "cj-");
            url = url.Replace("d-j-", "dj-");
            url = url.Replace("j-r-", "jr-");
            url = url.Replace("k-c-", "kc-");
            url = url.Replace("k-j-", "kj-");
            url = url.Replace("o-j-", "oj-");
            url = url.Replace("p-j-", "pj-");
            url = url.Replace("r-j-", "rj-");
            url = url.Replace("t-j-", "tj-");
            url = url.Replace("01horace-grant", "01-horace-grant");

            return url;
        }

        private static IEnumerable<Dictionary<string, object>> Get2KPlayers()
        {
            using (var stream = new FileStream(ConfigurationManager.AppSettings["GamePlayersFile"], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Read();
                        var headers = csv.FieldHeaders;

                        while (csv.Read())
                        {
                            var dictionary = new Dictionary<string, object>();

                            var row = csv.CurrentRecord;

                            int indexer = 0;

                            foreach (var header in headers)
                            {
                                if (!string.IsNullOrWhiteSpace(header))
                                {
                                    if (!dictionary.ContainsKey(header))
                                    {
                                        dictionary.Add(header, row[indexer]);
                                    }
                                }

                                indexer++;
                            }

                            yield return dictionary;
                        }
                    }
                }
            }
        }
    }
}
