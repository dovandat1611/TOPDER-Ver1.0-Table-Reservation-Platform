using System;
using System.Collections.Generic;

namespace Topder.Models
{
    public partial class Wishlist
    {
        public int WishlistId { get; set; }
        public int? RestaurantId { get; set; }
        public int? CustomerId { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual Restaurant? Restaurant { get; set; }
    }
}
