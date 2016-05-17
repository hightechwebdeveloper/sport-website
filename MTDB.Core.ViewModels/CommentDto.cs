using System.Collections.Generic;

namespace MTDB.Core.ViewModels
{
    public class CommentDto
    {
        public long Id { get; set; }

        public string UserName { get; set; }

        public string TimeAgo { get; set; }

        public string Text { get; set; }

        public IEnumerable<CommentDto> Children { get; set; }
    }
}
