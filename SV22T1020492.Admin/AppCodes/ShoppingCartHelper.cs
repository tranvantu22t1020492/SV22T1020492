using SV22T1020492.Admin;
using SV22T1020492.Models.Sales;

namespace SV22T1020362.Admin
{
    /// <summary>
    /// Lớp cung cấp các hàm tiện ích liên quan đến giỏ hàng
    /// (giỏ hàng lưu trong session)
    /// </summary>
    public class ShoppingCartHelper
    {
        /// <summary>
        /// Tên biến để lưu giỏ hàng trong session
        /// </summary>
        private const string CART = "ShoppingCart";

        /// <summary>
        /// Lấy giỏ hàng từ session (nếu giỏ hàng chưa có thì tạo giỏ hàng rỗng)
        /// </summary>
        /// <returns></returns>
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }
            return cart;
        }

        /// <summary>
        /// Lấy thông tin 1 mặt hàng trong giỏ
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public static OrderDetailViewInfo? GetCartItem(int productID)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            return item;
        }

        /// <summary>
        /// Thêm hàng vào giỏ
        /// </summary>
        /// <param name="data"></param>
        public static void AddItemToCart(OrderDetailViewInfo data)
        {
            var cart = GetShoppingCart();

            var existItem = cart.Find(m => m.ProductID == data.ProductID);
            if (existItem == null)
            {
                cart.Add(data);
            }
            else
            {
                existItem.Quantity += data.Quantity;
                existItem.SalePrice = data.SalePrice;
            }

            ApplicationContext.SetSessionData(CART, cart);
        }

        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong giỏ
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var existItem = cart.Find(m => m.ProductID == productID);

            if (existItem != null)
            {
                existItem.Quantity = quantity;
                existItem.SalePrice = salePrice;

                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa 1 mặt hàng khỏi giỏ dựa vào mã hàng
        /// </summary>
        /// <param name="productId"></param>
        public static void RemoveItemFromCart(int productId)
        {
            var cart = GetShoppingCart();

            //Tìm vị trí mặt hàng cần xóa trong giỏ
            int index = cart.FindIndex(m => m.ProductID == productId);

            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART, cart);
            }

        }

        /// <summary>
        /// Xóa trống giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            var cart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(CART, cart);
        }
    }
}
