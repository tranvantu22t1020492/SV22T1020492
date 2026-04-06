using System.Text.Json;
using SV22T1020492.Models.Sales;

namespace SV22T1020492.Shop.Models
{
    public static class CartHelper
    {
        private const string CART_KEY = "UserShoppingCart";

        public static List<OrderDetailViewInfo> GetCart(HttpContext context)
        {
            var json = context.Session.GetString(CART_KEY);
            return json != null ? JsonSerializer.Deserialize<List<OrderDetailViewInfo>>(json)! : new List<OrderDetailViewInfo>();
        }

        public static void SaveCart(HttpContext context, List<OrderDetailViewInfo> cart)
        {
            context.Session.SetString(CART_KEY, JsonSerializer.Serialize(cart));
        }

        public static void ClearCart(HttpContext context)
        {
            // Gán giá trị rỗng để chắc chắn không còn dữ liệu cũ trong Session
            context.Session.SetString(CART_KEY, "");
            context.Session.Remove(CART_KEY);
            // Xóa thêm ID đơn hàng để ép hệ thống tạo mới ở lần sau
            context.Session.Remove("OrderID");
        }
    }
}