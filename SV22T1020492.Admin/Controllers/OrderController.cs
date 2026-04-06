using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
using SV22T1020492.Models.Catalog;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Sales;

namespace SV22T1020492.Admin.Controllers
{
    public class OrderController : Controller
    {
        private const string SEARCH_PRODUCT = "SearchProduct";
        public const int PAGESIZE = 10;
        public const string SEARCH_ORDER = "SearchOrder";

        // =============================
        // 1. Danh sách đơn hàng
        // =============================
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(SEARCH_ORDER);
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = PAGESIZE,
                    SearchValue = "",
                    Status = 0,
                    DateFrom = null,
                    DateTo = null
                };
            }
            return View(input);
        }

        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            if (input == null) input = new OrderSearchInput { Page = 1, PageSize = PAGESIZE };

            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(SEARCH_ORDER, input);

            return View(result);
        }

        // =============================
        // 2. Lập đơn hàng
        // =============================
        public IActionResult Create()
        {
            return View();
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return View(result);
        }

        // =============================
        // 3. Chi tiết đơn hàng
        // =============================
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            order.Details = await SalesDataService.ListDetailsAsync(id);

            return View(order);
        }

        // =============================
        // 4. Quản lý trạng thái đơn
        // =============================

        public async Task<IActionResult> Accept(int id)
        {
            int employeeID = 1;
            bool result = await SalesDataService.AcceptOrderAsync(id, employeeID);
            if (result)
                TempData["Message"] = "Đã duyệt đơn hàng thành công.";
            else
                TempData["Error"] = "Không thể duyệt đơn hàng này.";

            return RedirectToAction("Detail", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            if (id <= 0) return NotFound();

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            ViewBag.OrderID = id;

            // SỬA TẠI ĐÂY: Thay PageSize = 0 thành PageSize = 100 (hoặc số lớn hơn)
            var input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 100, // Đảm bảo số này > 0 để tránh lỗi FETCH clause
                SearchValue = ""
            };

            var result = await PartnerDataService.ListShippersAsync(input);

            return PartialView(result.DataItems);
        }

        // Chuyển giao hàng: Bước 2 - Xử lý lưu (Giữ nguyên như đã hướng dẫn trước đó)
        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperID = 0)
        {
            if (id <= 0) return NotFound();

            if (shipperID <= 0)
            {
                TempData["Error"] = "Vui lòng chọn một đơn vị vận chuyển.";
                return RedirectToAction("Detail", new { id = id });
            }

            bool result = await SalesDataService.ShipOrderAsync(id, shipperID);

            if (result)
                TempData["Message"] = "Đơn hàng đã được chuyển cho đơn vị vận chuyển.";
            else
                TempData["Error"] = "Cập nhật thất bại. Vui lòng kiểm tra lại.";

            return RedirectToAction("Detail", new { id = id });
        }

        public async Task<IActionResult> Finish(int id)
        {
            bool result = await SalesDataService.CompleteOrderAsync(id);
            if (result)
                TempData["Message"] = "Đơn hàng đã hoàn tất thành công.";
            else
                TempData["Error"] = "Không thể hoàn tất đơn hàng.";

            return RedirectToAction("Detail", new { id });
        }

        public async Task<IActionResult> Reject(int id)
        {
            int employeeID = 1;
            bool result = await SalesDataService.RejectOrderAsync(id, employeeID);
            if (result)
                TempData["Message"] = "Đã từ chối đơn hàng.";
            else
                TempData["Error"] = "Không thể từ chối đơn hàng này.";

            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Hủy đơn hàng: Thực hiện xóa hoàn toàn đơn hàng khỏi database theo yêu cầu
        /// </summary>
        public async Task<IActionResult> Cancel(int id)
        {
            // Bước 1: Xóa tất cả mặt hàng chi tiết của đơn hàng này trước (tránh lỗi khóa ngoại)
            var details = await SalesDataService.ListDetailsAsync(id);
            foreach (var item in details)
            {
                await SalesDataService.DeleteDetailAsync(id, item.ProductID);
            }

            // Bước 2: Xóa đơn hàng chính khỏi cơ sở dữ liệu
            bool result = await SalesDataService.DeleteOrderAsync(id);

            if (result)
            {
                TempData["Message"] = "Đơn hàng đã được hủy và xóa khỏi hệ thống.";
                return RedirectToAction("Index"); // Quay về danh sách vì đơn không còn tồn tại để xem chi tiết
            }
            else
            {
                TempData["Error"] = "Không thể thực hiện hủy/xóa đơn hàng này.";
                return RedirectToAction("Detail", new { id });
            }
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            // Đảm bảo dọn dẹp chi tiết trước khi xóa đơn hàng chính
            var details = await SalesDataService.ListDetailsAsync(id);
            foreach (var item in details)
            {
                await SalesDataService.DeleteDetailAsync(id, item.ProductID);
            }

            bool result = await SalesDataService.DeleteOrderAsync(id);
            if (result)
                TempData["Message"] = "Đã xóa đơn hàng thành công.";
            else
                TempData["Error"] = "Không thể xóa đơn hàng (Chỉ được xóa đơn vừa khởi tạo hoặc đơn hợp lệ).";

            return RedirectToAction("Index");
        }

        // =============================
        // 5. Quản lý mặt hàng trong đơn
        // =============================
        public IActionResult EditCartItem(int id = 0, int productId = 0)
        {
            return View();
        }

        public async Task<IActionResult> DeleteCartItem(int id, int productId)
        {
            bool result = await SalesDataService.DeleteDetailAsync(id, productId);
            return RedirectToAction("Detail", new { id });
        }
    }
}