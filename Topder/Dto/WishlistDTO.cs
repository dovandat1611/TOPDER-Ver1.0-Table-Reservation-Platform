namespace Topder.Dto
{
    public class WishlistDTO
    {   
        public int WishlistId { get; set; }
        public int ResId { get; set; }
        public string Image { get; set; }
        public string NameRes { get; set; }
        public string CategoryName { get; set; }
        public int TotalReviews { get; set; }
        public int Star { get; set; }
    }
}
