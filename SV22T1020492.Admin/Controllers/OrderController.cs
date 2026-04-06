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
        private const string SHOPPING_CART = "ShoppingCart"; // Session lưu giỏ hàng

        // =============================
        // 1. Danh sách đơn hàng
        // =============================
        public async Task<IActionResult> Index()
        {
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var customerInput = new PaginationSearchInput() { Page = 1, PageSize = 100, SearchValue = "" };
            var customerResult = await PartnerDataService.ListCustomersAsync(customerInput);
            ViewBag.Customers = customerResult.DataItems;

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
        // 2. Lập đơn hàng (ĐÃ SỬA)
        // =============================
        public async Task<IActionResult> Create()
        {
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();

            // Sử dụng chính logic tìm kiếm đã có trong CatalogDataService
            var input = new ProductSearchInput { Page = 1, PageSize = 20, SearchValue = "" };
            var result = await CatalogDataService.ListProductsAsync(input);

            return View(result.DataItems); // Truyền danh sách sản phẩm (DataItems) sang View
        }

        // ================================================================
        // CÁC HÀM XỬ LÝ GIỎ HÀNG (Dùng cho AJAX trong Create.cshtml)
        // ================================================================

        public IActionResult GetCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(SHOPPING_CART) ?? new List<OrderDetailViewInfo>();
            return PartialView("Cart", cart);
        }

        [HttpPost]
        public IActionResult AddToCart(OrderDetailViewInfo item)
        {
            try
            {
                if (item.ProductID <= 0) return BadRequest("Mã sản phẩm không hợp lệ");

                var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(SHOPPING_CART) ?? new List<OrderDetailViewInfo>();

                var exists = cart.FirstOrDefault(m => m.ProductID == item.ProductID);
                if (exists == null)
                {
                    cart.Add(item);
                }
                else
                {
                    exists.Quantity += item.Quantity;
                    exists.SalePrice = item.SalePrice;
                }

                ApplicationContext.SetSessionData(SHOPPING_CART, cart);

                // Quan trọng: Phải có file _Cart.cshtml trong Views/Order/
                return PartialView("Cart", cart);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(SHOPPING_CART) ?? new List<OrderDetailViewInfo>();
            var item = cart.FirstOrDefault(m => m.ProductID == id);
            if (item != null) cart.Remove(item);

            ApplicationContext.SetSessionData(SHOPPING_CART, cart);
            return PartialView("Cart", cart);
        }

        public IActionResult ClearCart()
        {
            // SỬA TẠI ĐÂY: Dùng OrderDetailViewInfo cho đồng bộ với các hàm trên
            var emptyCart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(SHOPPING_CART, emptyCart);
            return PartialView("Cart", emptyCart);
        }

        [HttpGet] // Thêm Explicit HttpGet để chắc chắn
        public async Task<IActionResult> SearchProduct(string searchValue)
        {
            var input = new ProductSearchInput { Page = 1, PageSize = 100, SearchValue = searchValue ?? "" };
            var result = await CatalogDataService.ListProductsAsync(input);

            // Trả về PartialView để AJAX nạp vào div, không gây nhảy trang
            return PartialView("SearchProduct", result.DataItems);
        }
        // =============================
        // 3. Chi tiết đơn hàng
        // =============================
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return RedirectToAction("Index");
            order.Details = await SalesDataService.ListDetailsAsync(id);
            return View(order);
        }

        // =============================
        // 4. Quản lý trạng thái đơn (GIỮ NGUYÊN NỘI DUNG CŨ)
        // =============================
        public async Task<IActionResult> Accept(int id)
        {
            int employeeID = 1;
            bool result = await SalesDataService.AcceptOrderAsync(id, employeeID);
            if (result) TempData["Message"] = "Đã duyệt đơn hàng thành công.";
            else TempData["Error"] = "Không thể duyệt đơn hàng này.";
            return RedirectToAction("Detail", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            if (id <= 0) return NotFound();
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();
            ViewBag.OrderID = id;
            var input = new PaginationSearchInput() { Page = 1, PageSize = 100, SearchValue = "" };
            var result = await PartnerDataService.ListShippersAsync(input);
            return PartialView(result.DataItems);
        }

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
            if (result) TempData["Message"] = "Đơn hàng đã được chuyển cho đơn vị vận chuyển.";
            else TempData["Error"] = "Cập nhật thất bại. Vui lòng kiểm tra lại.";
            return RedirectToAction("Detail", new { id = id });
        }

        public async Task<IActionResult> Finish(int id)
        {
            bool result = await SalesDataService.CompleteOrderAsync(id);
            if (result) TempData["Message"] = "Đơn hàng đã hoàn tất thành công.";
            else TempData["Error"] = "Không thể hoàn tất đơn hàng.";
            return RedirectToAction("Detail", new { id });
        }

        public async Task<IActionResult> Reject(int id)
        {
            int employeeID = 1;
            bool result = await SalesDataService.RejectOrderAsync(id, employeeID);
            if (result) TempData["Message"] = "Đã từ chối đơn hàng.";
            else TempData["Error"] = "Không thể từ chối đơn hàng này.";
            return RedirectToAction("Detail", new { id });
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var details = await SalesDataService.ListDetailsAsync(id);
            foreach (var item in details) await SalesDataService.DeleteDetailAsync(id, item.ProductID);
            bool result = await SalesDataService.DeleteOrderAsync(id);
            if (result)
            {
                TempData["Message"] = "Đơn hàng đã được hủy và xóa khỏi hệ thống.";
                return RedirectToAction("Index");
            }
            TempData["Error"] = "Không thể thực hiện hủy/xóa đơn hàng này.";
            return RedirectToAction("Detail", new { id });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var details = await SalesDataService.ListDetailsAsync(id);
            foreach (var item in details) await SalesDataService.DeleteDetailAsync(id, item.ProductID);
            bool result = await SalesDataService.DeleteOrderAsync(id);
            if (result) TempData["Message"] = "Đã xóa đơn hàng thành công.";
            else TempData["Error"] = "Không thể xóa đơn hàng.";
            return RedirectToAction("Index");
        }

        // =============================
        // 5. Quản lý mặt hàng trong đơn
        // =============================
        [HttpGet]
        public async Task<IActionResult> EditCartItem(int id = 0, int productId = 0)
        {
            var detail = await SalesDataService.GetDetailAsync(id, productId);
            if (detail == null) return NotFound();

            // Chuyển đổi từ OrderDetailViewInfo sang OrderDetail (nếu cần)
            var model = new OrderDetail()
            {
                OrderID = detail.OrderID,
                ProductID = detail.ProductID,
                Quantity = detail.Quantity,
                SalePrice = detail.SalePrice
            };

            ViewBag.ProductName = detail.ProductName; // Để hiển thị tên trên Modal
            ViewBag.Photo = detail.Photo;
            return PartialView(model);
        }

        // 2. Action xử lý cập nhật vào Database
        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(OrderDetail data)
        {
            if (data.Quantity <= 0)
            {
                TempData["Error"] = "Số lượng không hợp lệ";
                return RedirectToAction("Detail", new { id = data.OrderID });
            }

            bool result = await SalesDataService.UpdateDetailAsync(data);
            if (result)
                TempData["Message"] = "Cập nhật sản phẩm thành công.";
            else
                TempData["Error"] = "Không thể cập nhật sản phẩm.";

            return RedirectToAction("Detail", new { id = data.OrderID });
        }

        public async Task<IActionResult> DeleteCartItem(int id, int productId)
        {
            bool result = await SalesDataService.DeleteDetailAsync(id, productId);
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Khởi tạo đơn hàng từ giỏ hàng Session vào Database (Theo logic của bạn bạn)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> InitOrder(string customerName, string deliveryProvince, string deliveryAddress)
        {
            // 1. Lấy giỏ hàng từ Session của BẠN (Dùng SHOPPING_CART thay vì CART_KEY)
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(SHOPPING_CART);

            if (cart == null || cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng trống, không thể lập đơn hàng.";
                return RedirectToAction("Create");
            }

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(deliveryProvince))
            {
                TempData["Error"] = "Vui lòng nhập tên khách hàng và chọn tỉnh thành.";
                return RedirectToAction("Create");
            }

            // 2. LOGIC TÌM KHÁCH HÀNG: Kiểm tra xem tên khách hàng đã tồn tại trong DB chưa
            var input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 100,
                SearchValue = customerName ?? ""
            };

            var customersResult = await PartnerDataService.ListCustomersAsync(input);

            // Tìm khách hàng đầu tiên khớp tên (không phân biệt hoa thường)
            var customer = customersResult.DataItems.FirstOrDefault(c =>
                c.CustomerName.Trim().ToLower() == customerName.Trim().ToLower());

            int? customerID = customer?.CustomerID; // Nếu tìm thấy thì lấy ID, không thì null

            // 3. Chuẩn bị dữ liệu mặt hàng
            int employeeID = 1; // Giả sử ID nhân viên xử lý là 1
            var orderDetails = cart.Select(item => new OrderDetail()
            {
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                SalePrice = item.SalePrice
            }).ToList();

            // 4. Gọi Service để lưu vào DB (OrderID sẽ tự động cộng 1 tại đây)
            int orderID = await SalesDataService.InitOrderAsync(employeeID, customerID, deliveryProvince, deliveryAddress, orderDetails);

            if (orderID > 0)
            {
                // 5. THÀNH CÔNG: Xóa giỏ hàng Session của bạn
                ApplicationContext.SetSessionData(SHOPPING_CART, new List<OrderDetailViewInfo>());

                TempData["Message"] = $"Đã lập đơn hàng #{orderID} thành công cho khách hàng {customerName}.";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Không thể lập đơn hàng. Vui lòng thử lại.";
            return RedirectToAction("Create");
        }
    }
}