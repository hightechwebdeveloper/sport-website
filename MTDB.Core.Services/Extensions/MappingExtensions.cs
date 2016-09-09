using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services.Extensions
{
    public static class MappingExtensions
    {
        static MappingExtensions()
        {
            Mapper.CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.TimeAgo, opt => opt.MapFrom(src => src.CreatedDate.ToTimeAgo()));
        }

        //public static SearchPlayerResultDto ToSearchDto(this Player player)
        //{
        //    if (player == null)
        //        return null;

        //    var position = player.PrimaryPosition;

        //    if (player.SecondaryPosition != null)
        //    {
        //        position = $"{position}/{player.SecondaryPosition}";
        //    }

        //    return new SearchPlayerResultDto
        //    {
        //        Id = player.Id,
        //        Name = player.Name,
        //        UriName = player.UriName,
        //        ImageUri = player.GetImageUri(ImageSize.PlayerSearch),
        //        Position = position,
        //        Tier = player.Tier.Name,
        //        Collection = player.Collection?.Name,
        //        Xbox = player.Xbox,
        //        PS4 = player.PS4,
        //        PC = player.PC,
        //        Height = player.Height,
        //        Overall = player.Overall,
        //        OutsideScoring = player.OutsideScoring.Value,
        //        InsideScoring = player.InsideScoring.Value,
        //        Playmaking = player.Playmaking.Value,
        //        Athleticism = player.Athleticism.Value,
        //        Defending = player.Defending.Value,
        //        Rebounding = player.Rebounding.Value,
        //        CreatedDate = player.CreatedDate,
        //        Prvate = player.Private
        //    };
        //}
        
        public static TierDto ToDto(this Tier tier)
        {
            if (tier == null)
                return null;

            return new TierDto { Id = tier.Id, Name = tier.Name };
        }
        
        public static LineupDto ToDto(this Lineup lineup)
        {
            var overall = 0;
            var outsideScoring = 0;
            var insideScoring = 0;
            var playmaking = 0;
            var athleticism = 0;
            var defending = 0;
            var rebounding = 0;

            if (lineup.Players.Any())
            {
                overall = lineup.Overall;
                outsideScoring = lineup.OutsideScoring;
                insideScoring = lineup.InsideScoring;
                playmaking = lineup.Playmaking;
                athleticism = lineup.Athleticism;
                defending = lineup.Defending;
                rebounding = lineup.Rebounding;
            }


            return new LineupDto
            {
                Id = lineup.Id,
                Name = lineup.Name,
                AuthorId = lineup.User?.Id,
                Author = lineup.User != null ? lineup.User.UserName : "Guest",
                Overall = overall,
                OutsideScoring = outsideScoring,
                InsideScoring = insideScoring,
                Playmaking = playmaking,
                Athleticism = athleticism,
                Defending = defending,
                Rebounding = rebounding,
                PointGuard = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.PointGuard).ToLineupPlayerDto(),
                ShootingGuard = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.ShootingGuard).ToLineupPlayerDto(),
                SmallForward = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.SmallForward).ToLineupPlayerDto(),
                PowerForward = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.PowerForward).ToLineupPlayerDto(),
                Center = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Center).ToLineupPlayerDto(),
                Bench1 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Bench1).ToLineupPlayerDto(),
                Bench2 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Bench2).ToLineupPlayerDto(),
                Bench3 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Bench3).ToLineupPlayerDto(),
                Bench4 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Bench4).ToLineupPlayerDto(),
                Bench5 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Bench5).ToLineupPlayerDto(),
                Bench6 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Bench6).ToLineupPlayerDto(),
                Bench7 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Bench7).ToLineupPlayerDto(),
                Bench8 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPositionType.Bench8).ToLineupPlayerDto(),
            };
        }

        public static LineupPlayerDto ToLineupPlayerDto(this LineupPlayer player)
        {
            if (player?.Player == null)
                return null;
            
            return new LineupPlayerDto
            {
                Name = player.Player.Name,
                Uri = player.Player.UriName,
                ImageUri = player.Player.GetImageUri(ImageSize.Full),
                Overall = player.Player.Overall,
                OutsideScoring = player.Player.OutsideScoring.Value,
                InsideScoring = player.Player.InsideScoring.Value,
                Playmaking = player.Player.Playmaking.Value,
                Athleticism = player.Player.Athleticism.Value,
                Defending = player.Player.Defending.Value,
                Rebounding = player.Player.Rebounding.Value,
                Private = player.Player.Private,
                Id = player.Player.Id
            };
        }

        public static IEnumerable<LineupSearchDto> ToSearchDtos(this IEnumerable<Lineup> lineups)
        {
            var dtos = new List<LineupSearchDto>();

            foreach (var lineup in lineups)
            {
                var name = lineup.Name;

                if (name.Length >= 50)
                {
                    name = name.Substring(0, 50) + "...";
                }


                dtos.Add(new LineupSearchDto
                {
                    Id = lineup.Id,
                    Name = name,
                    PlayerCount = lineup.PlayerCount,
                    Overall = lineup.Overall,
                    OutsideScoring = lineup.OutsideScoring,
                    InsideScoring = lineup.InsideScoring,
                    Playmaking = lineup.Playmaking,
                    Athleticism = lineup.Athleticism,
                    Defending = lineup.Defending,
                    Rebounding = lineup.Rebounding,
                    Xbox = lineup.Xbox,
                    PS4 = lineup.PS4,
                    PC = lineup.PC,
                    Author = lineup.User == null ? "Guest" : lineup.User.UserName,
                    CreatedDateString = lineup.CreatedDate.ToString("G")
                });
            }

            return dtos;
        }

        public static LineupPlayer ToLineupPlayer(this Player player, LineupPositionType position)
        {
            if (player == null)
                return null;

            return new LineupPlayer
            {
                Player = player,
                LineupPosition = position
            };
        }
        
        public static CommentDto ToDto(this Comment comment)
        {
            return Mapper.Map<CommentDto>(comment);
        }

    }
}
