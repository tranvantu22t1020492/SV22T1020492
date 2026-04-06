using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
using SV22T1020492.Models.Catalog;

namespace SV22T1020492.Shop.Controllers
{
    public class ProductController : Controller
    {
        // Chức năng 4: Tìm kiếm và lọc danh sách
        public IActionResult Index(string searchValue = "", int categoryID = 0, decimal minPrice = 0, decimal maxPrice = 0, int page = 1)
        {
            ViewBag.SearchValue = searchValue;
            ViewBag.CategoryID = categoryID;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Page = page;
            return View();
        }

        // ACTION MỚI: Chỉ trả về nội dung danh sách (PartialView)
        // Giữ nguyên Index cũ của bạn ở trên...

        public IActionResult Search(string searchValue = "", int categoryID = 0, decimal minPrice = 0, decimal maxPrice = 0, int page = 1)
        {
            // Quan trọng: Phải dùng ViewComponent để nó chỉ render ra cái danh sách sản phẩm
            return ViewComponent("ProductList", new
            {
                searchValue = searchValue,
                categoryID = categoryID,
                minPrice = minPrice,
                maxPrice = maxPrice,
                page = page
            });
        }
        // Chức năng 5: Xem chi tiết sản phẩm
        public async Task<IActionResult> Details(int id = 0)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index");
            }

            // Lấy thông tin mặt hàng chính
            var product = await CatalogDataService.GetProductAsync(id);

            if (product == null)
            {
                return RedirectToAction("Index");
            }

            // --- SỬA TÊN HÀM Ở ĐÂY ĐỂ KHỚP VỚI SERVICE ---
            // ListPhotoAsync (Không có 's')
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            // ListAttributesAsync
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }

    }
}