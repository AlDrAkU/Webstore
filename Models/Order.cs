using System.ComponentModel.DataAnnotations;

namespace Webstore.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; }
        public IList<OrderDetail> ?OrderDetails { get; set; }
        public string Status { get; set; } // "Cart" or "Submitted"
        public string ?UserId { get; set; }
    }
}
