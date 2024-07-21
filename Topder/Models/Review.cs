using System;
using System.Collections.Generic;

namespace Topder.Models
{
    public partial class Review
    {
        public int RateId { get; set; }
        public int? CustomerId { get; set; }
        public int? RestaurantId { get; set; }
        public int? Star { get; set; }
        public string? Content { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? Status { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual Restaurant? Restaurant { get; set; }
    }
}
