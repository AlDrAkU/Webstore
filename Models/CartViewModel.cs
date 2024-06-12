namespace Webstore.Models
{
    public class CartViewModel
    {
        public List<OrderDetail> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }

}
