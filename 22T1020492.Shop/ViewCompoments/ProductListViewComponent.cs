using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
using SV22T1020492.Models.Catalog;

namespace SV22T1020492.Shop.ViewComponents
{
    public class ProductListViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(
            string searchValue = "",
            int categoryID = 0,
            decimal minPrice = 0,
            decimal maxPrice = 0,
            int page = 1,        // 1. Phải có tham số này nhận từ View
            int pageSize = 20)
        {
            var input = new ProductSearchInput()
            {
                Page = page,      // 2. PHẢI GÁN GIÁ TRỊ NÀY VÀO ĐÂY
                PageSize = pageSize,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SupplierID = 0
            };

            // 3. Gán lại vào ViewBag để hiển thị đúng số trang đang active ở View
            ViewBag.CurrentPage = page;
            ViewBag.CurrentSearchValue = searchValue;
            ViewBag.CurrentCategoryID = categoryID;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;

            var result = await CatalogDataService.ListProductsAsync(input);
            return View(result);
        }
    }
}