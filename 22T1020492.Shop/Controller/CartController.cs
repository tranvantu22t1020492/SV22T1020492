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
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);

            if (cartOrder == null || (int)cartOrder.Status != 0)
            {
                return View(new List<OrderDetailViewInfo>());
            }

            var model = await SalesDataService.ListDetailsAsync(cartOrder.OrderID);
            HttpContext.Session.SetInt32("CartCount", model.Sum(i => i.Quantity));

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productID, int quantity = 1, decimal salePrice = 0)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

            if (quantity <= 0) quantity = 1;

            if (salePrice <= 0)
            {
                var product = await CatalogDataService.GetProductAsync(productID);
                if (product != null) salePrice = product.Price;
            }


            bool result = await SalesDataService.AddToCartAsync(userId.Value, productID, quantity, salePrice);

            if (result)
            {

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
            var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);

            if (cartOrder == null || (int)cartOrder.Status != 0) return 0;

            var details = await SalesDataService.ListDetailsAsync(cartOrder.OrderID);
            int total = details.Sum(i => i.Quantity);

            HttpContext.Session.SetInt32("CartCount", total);

            return total;
        }

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
                    {
                        var product = await CatalogDataService.GetProductAsync(productID);
                        decimal currentPrice = product != null ? product.Price : 0;

                        await SalesDataService.UpdateDetailAsync(new OrderDetail
                        {
                            OrderID = cartOrder.OrderID,
                            ProductID = productID,
                            Quantity = quantity,
                            SalePrice = currentPrice 
                        });
                    }
                    else
                    {
                        await SalesDataService.DeleteDetailAsync(cartOrder.OrderID, productID);
                    }
                }
            }
            return RedirectToAction("Index");
        }

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

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {

                await SalesDataService.ClearCartAsync(userId.Value);


                HttpContext.Session.SetInt32("CartCount", 0);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false });

            var cartOrder = await SalesDataService.GetCartOrderAsync(userId.Value);
            if (cartOrder == null) return Json(new { success = false });

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