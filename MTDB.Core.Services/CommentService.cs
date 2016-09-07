using AutoMapper;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Services.Extensions;

namespace MTDB.Core.Services
{
    public class CommentService
    {
        private readonly EntityFramework.MtdbContext _repository;

        public CommentService(EntityFramework.MtdbContext repository)
        {
            _repository = repository;
        }

        public async Task<CommentsViewModel> GetComments(string pageUrl, CancellationToken token)
        {
            var result  = await _repository.Comments
                                    .Include(c => c.User)
                                    .Where(x => x.PageUrl == pageUrl)
                                    .OrderBy(x => x.CreatedDate)
                                    .ToListAsync(token);

            if (!result.HasItems())
            {
                return new CommentsViewModel
                {
                    PageUrl = pageUrl
                };
            }

            var aggregatedResult = result.Where(x => x.ParentId == null).Select(x =>
            {
                var comment = x.ToDto();
                comment.Children = result.Where(y => y.ParentId == x.Id).Select(sub => sub.ToDto());
                return comment;
            });

            return new CommentsViewModel
            {
                PageUrl = pageUrl,
                Total = result.Count(),
                Comments = aggregatedResult.ToList()
            };
        }

        public async Task<long> CreateComment(Comment comment, CancellationToken token)
        {
            if (comment.Id != 0)
            {
                throw new Exception($"Comment {comment.Id} already exists in the database.");
            }

            comment.Text = comment.Text.ReplaceBlockedWordsWithMTDB();

            if (!comment.Text.HasValue())
                return 0;

            _repository.Comments.Add(comment);

            await _repository.SaveChangesAsync(token);

            return comment.Id;
        }
    }

    public class CommentsViewModel
    {
        public CommentsViewModel()
        {
            Comments = Enumerable.Empty<CommentDto>().ToList();
        }

        public string PageUrl { get; set; }

        public int Total { get; set; }

        public List<CommentDto> Comments { get; set; }
    }
}
