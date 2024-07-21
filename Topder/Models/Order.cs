using System;
using System.Collections.Generic;

namespace Topder.Models
{
    public partial class Order
    {
        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
        public int? RestaurantId { get; set; }
        public string NameReceiver { get; set; } = null!;
        public string PhoneReceiver { get; set; } = null!;
        public string? Content { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime Date { get; set; }
        public int NumberPerson { get; set; }
        public int NumberChild { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? AccpectDate { get; set; }
        public DateTime? CancelDate { get; set; }
        public DateTime? ProcessDate { get; set; }
        public DateTime? DoneDate { get; set; }
        public string? Statusorder { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? ContentPayment { get; set; }
        public string? StatusPayment { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual Restaurant? Restaurant { get; set; }
    }
}
