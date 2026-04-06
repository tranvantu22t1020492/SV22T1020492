using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Partner;

namespace SV22T1020492.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator}")]
    public class CustomerController : Controller
    {
        public const string SEARCH_CUSTOMER = "SearchCustomer";
        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả
        /// </summary>
        /// <param name="page"></param>
        /// <param name="searchvalue"></param>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_CUSTOMER);
            if(input == null)
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
            var result = await PartnerDataService.ListCustomersAsync(input);

            // Lưu lại điều kiện tìm kiếm vào Session
            ApplicationContext.SetSessionData(SEARCH_CUSTOMER, input);

            // Trả về kết quả cho View
            return View(result);
        }
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
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
        public async Task<IActionResult> SaveData(Customer data)
        {
            ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";
            //TODO: Kiểm tra dữ liệu đầu vào có hợp lệ hay không?
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên khách hàng" );
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Hãy cho biết Email khách hàng");
            else if (!(await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID)))
                ModelState.AddModelError(nameof(data.Email), "Email này đã có người sử dụng");

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
            if (data.CustomerID == 0)
            {
                await PartnerDataService.AddCustomerAsync(data);       
            }
            else
            {
                await PartnerDataService.UpdateCustomerAsync(data);
            }
            PaginationSearchInput input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = data.CustomerName
            };
            ApplicationContext.SetSessionData(SEARCH_CUSTOMER, input);
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.Title = "Đổi mật khẩu khách hàng";
            return View(model);
        }

        /// <summary>
        /// Xử lý đổi mật khẩu cho khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Đổi mật khẩu khách hàng";

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
            else if (newPassword.Length < 6)
                ModelState.AddModelError("newPassword", "Mật khẩu phải có ít nhất 6 ký tự");

            if (string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận mật khẩu mới");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await SecurityDataService.ChangeCustomerPasswordAsync(model.Email, CryptHelper.HashMD5(newPassword));
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            bool allowDelete = !(await PartnerDataService.IsUsedCustomerAsync(id));
            ViewBag.AllowDelete = allowDelete; 
            return View(model);
        }
    }
}
