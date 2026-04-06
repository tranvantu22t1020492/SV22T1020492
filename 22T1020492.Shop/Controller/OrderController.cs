using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
using SV22T1020492.Models.Sales;
using SV22T1020492.Models.Partner;
using Microsoft.AspNetCore.Http;

namespace SV22T1020492.Shop.Controllers
{
    public class OrderController : Controller
    {
        public async Task<IActionResult> Index(int page = 1, int status = 0, string searchValue = "")
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            int pageSize = 5;
            var result = await SalesDataService.ListOrdersAsync(new OrderSearchInput
            {
                Page = 1,
                PageSize = 10000,
                Status = (OrderStatusEnum)status,
                SearchValue = searchValue ?? ""
            });

            var allUserOrders = result.DataItems
                                      .Where(x => x.CustomerID == userId.Value && (int)x.Status != 0)
                                      .OrderByDescending(x => x.OrderTime)
                                      .ToList();

            int totalItems = allUserOrders.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pagedOrders = allUserOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentStatus = status;
            ViewBag.SearchValue = searchValue;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedOrders);
        }

        public async Task<IActionResult> Details(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderAsync(id);


            if (order == null || order.CustomerID != userId.Value)
                return RedirectToAction("Index");

            var details = await SalesDataService.ListDetailsAsync(id);

            var productNames = new Dictionary<int, string>();
            foreach (var item in details)
            {
                var p = await CatalogDataService.GetProductAsync(item.ProductID);
                if (p != null) productNames[item.ProductID] = p.ProductName;
            }


            if (order.ShipperID.HasValue)
            {
                var shipper = await PartnerDataService.GetShipperAsync(order.ShipperID.Value);
                ViewBag.ShipperName = shipper?.ShipperName;
                ViewBag.ShipperPhone = shipper?.Phone;
            }


            var customer = await PartnerDataService.GetCustomerAsync((int)order.CustomerID);
            ViewBag.CustomerName = customer?.CustomerName;
            ViewBag.CustomerPhone = customer?.Phone;

            ViewBag.ProductNames = productNames;
            ViewBag.Order = order;
            return View(details);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var order = await SalesDataService.GetOrderAsync(id);


            if (order != null && order.CustomerID == userId && (int)order.Status == 1)
            {

                var details = await SalesDataService.ListDetailsAsync(id);
                foreach (var item in details)
                {
                    await SalesDataService.DeleteDetailAsync(id, item.ProductID);
                }
                await SalesDataService.DeleteOrderAsync(id);
                return RedirectToAction("Index");
            }
            return RedirectToAction("Details", new { id = id });
        }

        [HttpGet]
        public async Task<int> GetOrderCount()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return 0;

            var result = await SalesDataService.ListOrdersAsync(new OrderSearchInput
            {
                Page = 1,
                PageSize = 10000,
                Status = 0 
            });
            return result.DataItems.Count(x => x.CustomerID == userId.Value && (int)x.Status != 0);
        }


    }
}