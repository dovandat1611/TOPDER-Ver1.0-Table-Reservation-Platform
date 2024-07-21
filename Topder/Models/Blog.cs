using System;
using System.Collections.Generic;

namespace Topder.Models
{
    public partial class Blog
    {
        public int BlogId { get; set; }
        public int? BloggroupId { get; set; }
        public int? AdminId { get; set; }
        public string? Image { get; set; }
        public string? Title { get; set; }
        public string Content { get; set; } = null!;
        public DateTime? CreateDate { get; set; }
        public string? Status { get; set; }

        public virtual Admin? Admin { get; set; }
        public virtual BlogGroup? Bloggroup { get; set; }
    }
}
