using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
using SV22T1020492.Models.Sales;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SV22T1020492.Shop.Controllers
{
    public class CartController : Controller
    {
        // Hiển thị giỏ hàng (Status = 0)
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);

            // SỬA TẠI ĐÂY: Dùng ép kiểu (int)0 để chắc chắn lọc đúng giỏ hàng nháp
            if (cartOrder == null || (int)cartOrder.Status != 0)
            {
                return View(new List<OrderDetailViewInfo>());
            }

            var model = await SalesDataService.ListDetailsAsync(cartOrder.OrderID);

            // Cập nhật lại Badge số lượng trên Header mỗi khi vào giỏ hàng
            HttpContext.Session.SetInt32("CartCount", model.Sum(i => i.Quantity));

            return View(model);
        }

        // Thêm sản phẩm (HttpPost)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productID, int quantity = 1, decimal salePrice = 0)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

            if (quantity <= 0) quantity = 1;

            // Nếu giá bằng 0, cố gắng lấy giá từ Database
            if (salePrice <= 0)
            {
                var product = await CatalogDataService.GetProductAsync(productID);
                if (product != null) salePrice = product.Price;
            }

            // Thực hiện thêm vào giỏ
            bool result = await SalesDataService.AddToCartAsync(userId.Value, productID, quantity, salePrice);

            if (result)
            {
                // Cập nhật lại số lượng hiển thị trên icon giỏ hàng (Badge)
                var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);
                if (cartOrder != null)
                {
                    var details = await SalesDataService.ListDetailsAsync(cartOrder.OrderID);
                    int totalQty = details.Sum(i => i.Quantity);
                    HttpContext.Session.SetInt32("CartCount", totalQty);
                    return Json(new { success = true, cartCount = totalQty });
                }
            }

            return Json(new { success = result });
        }

        [HttpGet]
        public async Task<int> GetCartCount()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return 0;

            // Tận dụng hàm GetCartOrderAsync bạn đã có
            var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);

            // Nếu không có giỏ hàng (Status 0) thì trả về 0
            if (cartOrder == null || (int)cartOrder.Status != 0) return 0;

            var details = await SalesDataService.ListDetailsAsync(cartOrder.OrderID);
            int total = details.Sum(i => i.Quantity);

            // Cập nhật session để đồng bộ
            HttpContext.Session.SetInt32("CartCount", total);

            return total;
        }

        // Cập nhật số lượng (HttpPost)
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productID, int quantity)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);
                if (cartOrder != null)
                {
                    if (quantity > 0)
                        await SalesDataService.UpdateDetailAsync(new OrderDetail { OrderID = cartOrder.OrderID, ProductID = productID, Quantity = quantity });
                    else
                        await SalesDataService.DeleteDetailAsync(cartOrder.OrderID, productID);
                }
            }
            return RedirectToAction("Index");
        }

        // Xóa một hàng (HttpPost)
        [HttpPost]
        public async Task<IActionResult> Remove(int productID)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);
                if (cartOrder != null)
                {
                    await SalesDataService.DeleteDetailAsync(cartOrder.OrderID, productID);
                }
            }
            return RedirectToAction("Index");
        }

        // Xóa sạch giỏ (HttpPost)
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                // Phải đảm bảo hàm này trong Business Layer chỉ xóa đơn hàng có Status = 0
                await SalesDataService.ClearCartAsync(userId.Value);

                // Reset lại Session để tránh hiển thị số cũ
                HttpContext.Session.SetInt32("CartCount", 0);
            }
            return RedirectToAction("Index");
        }

        // Chốt đơn hàng (Checkout) - Chuyển Status 0 sang 1
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false });

            var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);
            if (cartOrder == null) return Json(new { success = false });

            // Chỉ cập nhật lại Status và thời gian đặt hàng
            var order = new Order
            {
                OrderID = cartOrder.OrderID,
                CustomerID = userId.Value,
                OrderTime = DateTime.Now,
                Status = (OrderStatusEnum)1, // 1: Đơn hàng vừa gửi
                // Giữ nguyên các thông tin cũ của đơn hàng nháp
                DeliveryProvince = cartOrder.DeliveryProvince ?? "",
                DeliveryAddress = cartOrder.DeliveryAddress ?? ""
            };

            bool success = await SalesDataService.UpdateOrderAsync(order);
            if (success) HttpContext.Session.SetInt32("CartCount", 0);

            return Json(new { success = success });
        }
    }
}