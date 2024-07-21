using System;
using System.Collections.Generic;

namespace Topder.Models
{
    public partial class Restaurant
    {
        public Restaurant()
        {
            Images = new HashSet<Image>();
            Orders = new HashSet<Order>();
            Reviews = new HashSet<Review>();
            Wishlists = new HashSet<Wishlist>();
        }

        public int ResId { get; set; }
        public int? CategoryId { get; set; }
        public string NameOwner { get; set; } = null!;
        public string NameRes { get; set; } = null!;
        public string? Image { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Description { get; set; }
        public string? Subdescription { get; set; }
        public string Location { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public decimal? Price { get; set; }
        public string? Discount { get; set; }
        public string? Status { get; set; }

        public virtual Category? Category { get; set; }
        public virtual ICollection<Image> Images { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
    }
}
