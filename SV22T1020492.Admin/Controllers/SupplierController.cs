using Microsoft.AspNetCore.Mvc;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Partner;

namespace SV22T1020492.Admin.Controllers
{
    public class SupplierController : Controller
    {
        public const string SEARCHS_SUPPLIER = "SearchSupplier";
        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả
        /// </summary>
        /// <param name="page"></param>
        /// <param name="searchvalue"></param>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCHS_SUPPLIER);
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
            var result = await PartnerDataService.ListSuppliersAsync(input);

            // Lưu lại điều kiện tìm kiếm vào Session
            ApplicationContext.SetSessionData(SEARCHS_SUPPLIER, input);

            // Trả về kết quả cho View
            return View(result);
        }
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit", model);
        }
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
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
        public async Task<IActionResult> SaveData(Supplier data)
        {
            ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật thông tin nhà cung cấp";
            //TODO: Kiểm tra dữ liệu đầu vào có hợp lệ hay không?
            if (string.IsNullOrWhiteSpace(data.SupplierName))
                ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên nhà cung cấp");
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Hãy cho biết Email nhà cung cấp");

            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành");
            if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = "";
            if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
            if (string.IsNullOrEmpty(data.Address)) data.Address = "";
            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }
            //Lưu dữ liệu vào CSDL
            if (data.SupplierID == 0)
            {
                await PartnerDataService.AddSupplierAsync(data);
            }
            else
            {
                await PartnerDataService.UpdateSupplierAsync(data);
            }
            PaginationSearchInput input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = data.SupplierName
            };
            ApplicationContext.SetSessionData(SEARCHS_SUPPLIER, input);
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteSupplierAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            bool allowDelete = !(await PartnerDataService.IsUsedSupplierAsync(id));
            ViewBag.AllowDelete = allowDelete;
            return View(model);
        }
    }
}
