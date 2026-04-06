using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.DataLayers.SQLServer;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Sales;

namespace SV22T1020492.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        public static async Task<int> AddOrderAsync(Order data)
        {
            data.Status = OrderStatusEnum.New;
            data.OrderTime = DateTime.Now;

            return await orderDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            //TODO: Kiểm tra dữ liệu và trạng thái đơn hàng trước khi cập nhật
            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            //TODO: Kiểm tra trạng thái đơn hàng trước khi xóa
            return await orderDB.DeleteAsync(orderID);
        }

        #endregion

        #region Giỏ hàng Database (Status = 0)

        private const int SHOPPING_CART_STATUS = 0; 

        /// <summary>
        /// Lấy thông tin đơn hàng đang đóng vai trò là giỏ hàng (Chỉ lấy Status = 0 và đúng CustomerID)
        /// </summary>
        public static async Task<OrderViewInfo?> GetCartOrderAsync(int customerID)
        {

            var input = new OrderSearchInput
            {
                CustomerID = customerID,
                Status = (OrderStatusEnum)SHOPPING_CART_STATUS, 
                PageSize = 1,
                Page = 1,
                SearchValue = "" 
            };

            var result = await orderDB.ListAsync(input);


            var cart = result.DataItems.FirstOrDefault(x => (int)x.Status == SHOPPING_CART_STATUS);
            return cart;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm trong giỏ để hiển thị (Sử dụng CartItem model của bạn)
        /// </summary>
        public static async Task<List<CartItem>> ListCartAsync(int customerID)
        {
            var cartOrder = await GetCartOrderAsync(customerID);
            if (cartOrder == null) return new List<CartItem>();

            var details = await orderDB.ListDetailsAsync(cartOrder.OrderID);
            return details.Select(x => new CartItem
            {
                ProductID = x.ProductID,
                ProductName = x.ProductName,
                Photo = x.Photo,
                Unit = x.Unit,
                Quantity = x.Quantity,
                SalePrice = x.SalePrice
            }).ToList();
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>
        public static async Task<bool> AddToCartAsync(int customerID, int productID, int quantity, decimal salePrice)
        {
            var cartOrder = await GetCartOrderAsync(customerID);
            int orderID;

            if (cartOrder == null)
            {
                orderID = await orderDB.AddAsync(new Order
                {
                    CustomerID = customerID,
                    OrderTime = DateTime.Now,
                    Status = (OrderStatusEnum)SHOPPING_CART_STATUS,
                    DeliveryProvince = "",
                    DeliveryAddress = ""
                });
            }
            else
            {
                orderID = cartOrder.OrderID;
            }

            var detail = await orderDB.GetDetailAsync(orderID, productID);
            if (detail != null)
            {
                detail.Quantity += quantity;
                detail.SalePrice = salePrice;
                return await orderDB.UpdateDetailAsync(detail);
            }

            return await orderDB.AddDetailAsync(new OrderDetail
            {
                OrderID = orderID,
                ProductID = productID,
                Quantity = quantity,
                SalePrice = salePrice
            });
        }

        /// <summary>
        /// Xóa bỏ hoàn toàn giỏ hàng
        /// </summary>
        public static async Task<bool> ClearCartAsync(int customerID)
        {
            var cartOrder = await GetCartOrderAsync(customerID);
            if (cartOrder != null)
                return await orderDB.DeleteAsync(cartOrder.OrderID);
            return false;
        }

        public static async Task<int> InitOrderAsync(int employeeID, int? customerID, string deliveryProvince, string deliveryAddress, List<OrderDetail> details)
        {
            var orderData = new Order()
            {
                // KHÔNG gán OrderID ở đây. SQL Server sẽ tự cấp ID (ví dụ: 10, 11, 12...)
                OrderTime = DateTime.Now,
                EmployeeID = employeeID,
                CustomerID = customerID,
                DeliveryProvince = deliveryProvince ?? "",
                DeliveryAddress = deliveryAddress ?? "",
                Status = OrderStatusEnum.New // Status = 1
            };

            // Hàm AddAsync này sẽ thực hiện INSERT và trả về cái ID vừa được tự động cộng 1
            int newOrderID = await orderDB.AddAsync(orderData);

            if (newOrderID > 0)
            {
                foreach (var item in details)
                {
                    // Sau khi có ID mới từ Database, ta mới gán nó vào các mặt hàng chi tiết
                    item.OrderID = newOrderID;
                    await orderDB.AddDetailAsync(item);
                }
                return newOrderID;
            }
            return 0;
        }

        #endregion

        #region Order Status Processing

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Giao đơn hàng cho người giao hàng
        /// </summary>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.Accepted)
                return false;

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.Shipping)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;
            
            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        /// <summary>
        /// Lấy danh sách mặt hàng của đơn hàng
        /// </summary>
        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng
        /// </summary>
        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            //TODO: Kiểm tra dữ liệu và trạng thái đơn hàng trước khi thêm mặt hàng
            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            //TODO: Kiểm tra dữ liệu và trạng thái đơn hàng trước khi cập nhật mặt hàng
            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            //TODO: Kiểm tra trạng thái đơn hàng trước khi xóa mặt hàng
            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion
    }
}