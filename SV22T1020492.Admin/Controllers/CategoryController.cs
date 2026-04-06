using SV22T1020492.BusinessLayers;
using Microsoft.AspNetCore.Mvc;
using SV22T1020492.Models.Catalog;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Partner;
using Microsoft.AspNetCore.Authorization;

namespace SV22T1020492.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator}")]
    public class CategoryController : Controller
    {
            public const string SEARCHS_CATEGORY = "SearchCategory";
            /// <summary>
            /// Nhập đầu vào tìm kiếm và hiển thị kết quả
            /// </summary>
            /// <param name="page"></param>
            /// <param name="searchvalue"></param>
            /// <returns></returns>
            public IActionResult Index()
            {
                var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCHS_CATEGORY);
                if (input == null)
                {
                    input = new PaginationSearchInput()
                    {
                        Page = 1,
                        PageSize = ApplicationContext.PageSize,
                        SearchValue = ""
                    };
                }
                return View(input);
            }
            public async Task<IActionResult> Search(PaginationSearchInput input)
            {
                var result = await CatalogDataService.ListCategoriesAsync(input);

                // Lưu lại điều kiện tìm kiếm vào Session
                ApplicationContext.SetSessionData(SEARCHS_CATEGORY, input);

                // Trả về kết quả cho View
                return View(result);
            }
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            var model = new Category()
            {
                CategoryID = 0
            };
            return View("Edit", model);
        }
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật thông tin loại hàng";
            //TODO: Kiểm tra dữ liệu đầu vào có hợp lệ hay không?
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng");
            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }
            //Lưu dữ liệu vào CSDL
            if (data.CategoryID == 0)
            {
                await CatalogDataService.AddCategoryAsync(data);
            }
            else
            {
                await CatalogDataService.UpdateCategoryAsync(data);
            }
            PaginationSearchInput input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = data.CategoryName
            };
            ApplicationContext.SetSessionData(SEARCHS_CATEGORY, input);
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            bool allowDelete = !(await CatalogDataService.IsUsedCategoryAsync(id));
            ViewBag.AllowDelete = allowDelete;
            return View(model);
        }
    }
}
