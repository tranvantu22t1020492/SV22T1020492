using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Partner;

namespace SV22T1020492.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator}")]

    public class ShipperController : Controller
    {
        public const string SEARCHS_SHIPPER = "SearchShipper";
        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả
        /// </summary>
        /// <param name="page"></param>
        /// <param name="searchvalue"></param>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCHS_SHIPPER);
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
            var result = await PartnerDataService.ListShippersAsync(input);

            // Lưu lại điều kiện tìm kiếm vào Session
            ApplicationContext.SetSessionData(SEARCHS_SHIPPER, input);

            // Trả về kết quả cho View
            return View(result);
        }
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View("Edit", model);
        }
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
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
        public async Task<IActionResult> SaveData(Shipper data)
        {
            ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật thông tin người giao hàng";
            //TODO: Kiểm tra dữ liệu đầu vào có hợp lệ hay không?
            if (string.IsNullOrWhiteSpace(data.ShipperName))
                ModelState.AddModelError(nameof(data.ShipperName), "Vui lòng nhập tên người giao hàng");
            if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }
            //Lưu dữ liệu vào CSDL
            if (data.ShipperID == 0)
            {
                await PartnerDataService.AddShipperAsync(data);
            }
            else
            {
                await PartnerDataService.UpdateShipperAsync(data);
            }
            PaginationSearchInput input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = data.ShipperName
            };
            ApplicationContext.SetSessionData(SEARCHS_SHIPPER, input);
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteShipperAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            bool allowDelete = !(await PartnerDataService.IsUsedShipperAsync(id));
            ViewBag.AllowDelete = allowDelete;
            return View(model);
        }
    }
}
