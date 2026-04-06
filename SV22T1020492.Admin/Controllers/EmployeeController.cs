using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.HR;
using SV22T1020492.DataLayers;
using Microsoft.AspNetCore.Authorization;

namespace SV22T1020492.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator}")]
    public class EmployeeController : Controller
    {
        public const string SEARCHS_EMPLOYEE = "SearchEmployee";
        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả
        /// </summary>
        /// <param name="page"></param>
        /// <param name="searchvalue"></param>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCHS_EMPLOYEE);
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
            var result = await HRDataService.ListEmployeesAsync(input);

            ApplicationContext.SetSessionData(SEARCHS_EMPLOYEE, input);

            return View(result);
        }
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data)
        {
            ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

            if (string.IsNullOrWhiteSpace(data.FullName))
                ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập tên nhân viên");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Hãy cho biết Email nhân viên");
            else if (!(await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID)))
                ModelState.AddModelError(nameof(data.Email), "Email này đã có người sử dụng");

            if (data.BirthDate == null)
            {
                ModelState.AddModelError(nameof(data.BirthDate), "Vui lòng nhập ngày sinh");
            }
            else
            {
                if (data.BirthDate > DateTime.Now)
                {
                    ModelState.AddModelError(nameof(data.BirthDate), "Ngày sinh không hợp lệ");
                }
                else
                {
                    int age = DateTime.Now.Year - data.BirthDate.Value.Year;
                    if (data.BirthDate > DateTime.Now.AddYears(-age)) age--;

                    if (age < 18 || age > 60)
                    {
                        ModelState.AddModelError(nameof(data.BirthDate), "Tuổi phải từ 18 đến 60");
                    }
                }
            }

            if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
            if (string.IsNullOrEmpty(data.Address)) data.Address = "";

            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }

            if (data.EmployeeID == 0)
            {
                await HRDataService.AddEmployeeAsync(data);
            }
            else
            {
                await HRDataService.UpdateEmployeeAsync(data);
            }

            PaginationSearchInput input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = data.FullName
            };

            ApplicationContext.SetSessionData(SEARCHS_EMPLOYEE, input);
            return RedirectToAction("Index");
        }
        //public async Task<IActionResult> ChangePassword(int id)
        //{
        //    ViewBag.Title = "Đổi mật khẩu nhân viên";

        //    var employee = await HRDataService.GetEmployeeAsync(id);
        //    if (employee == null)
        //        return RedirectToAction("Index");

        //    var model = new ChangePasswordEmployee()
        //    {
        //        EmployeeID = employee.EmployeeID,
        //        FullName = employee.FullName,
        //        Email = employee.Email,
        //        IsWorking = employee.IsWorking
        //    };

        //    return View(model);
        //}
        //[HttpPost]
        //public async Task<IActionResult> ChangePassword(ChangePasswordEmployee data)
        //{
        //    ViewBag.Title = "Đổi mật khẩu nhân viên";

        //    var employee = await HRDataService.GetEmployeeAsync(data.EmployeeID);
        //    if (employee == null)
        //        return RedirectToAction("Index");

        //    if (string.IsNullOrWhiteSpace(data.NewPassword))
        //        ModelState.AddModelError(nameof(data.NewPassword), "Vui lòng nhập mật khẩu mới");

        //    if (string.IsNullOrWhiteSpace(data.ConfirmPassword))
        //        ModelState.AddModelError(nameof(data.ConfirmPassword), "Vui lòng xác nhận mật khẩu");

        //    if (data.NewPassword != data.ConfirmPassword)
        //        ModelState.AddModelError(nameof(data.ConfirmPassword), "Mật khẩu xác nhận không khớp");

        //    if (employee.IsWorking)
        //        ModelState.AddModelError("", "Tài khoản đang bị khóa, không thể đổi mật khẩu");
        //    if (!ModelState.IsValid)
        //    {
        //        return View(data);
        //    }

        //    await HRDataService.ChangeEmployeePasswordAsync(data.Email, data.NewPassword);

        //    return RedirectToAction("Index");
        //}
        //[HttpGet]
        //public async Task<IActionResult> ChangeRoles(int id)
        //{
        //    ViewBag.Title = "Phân quyền nhân viên";

        //    var employee = await HRDataService.GetEmployeeAsync(id);
        //    if (employee == null)
        //        return RedirectToAction("Index");
        //    var currentRoles = (employee.RoleNames ?? "")
        //                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //                        .Select(r => r.Trim())
        //                        .ToList();

        //    var model = new ChangeRole()
        //    {
        //        EmployeeID = employee.EmployeeID,
        //        FullName = employee.FullName,
        //        Email = employee.Email,
        //        IsWorking = employee.IsWorking,

        //        Roles = HRDataService.GetRoleList().Select(r => new RoleItem
        //        {
        //            RoleName = r.RoleName,
        //            Description = r.Description,
        //            IsSelected = currentRoles.Any(cr => cr.Equals(r.RoleName.Trim(), StringComparison.OrdinalIgnoreCase))
        //        }).ToList()
        //    };

        //    if (model.IsWorking)
        //    {
        //        ModelState.AddModelError("", "Nhân viên đã nghỉ việc, không thể thực hiện phân quyền");
        //    }

        //    return View("ChangeRole", model);
        //}

        //[HttpPost]
        //public async Task<IActionResult> ChangeRole(ChangeRole data)
        //{
        //    ViewBag.Title = "Phân quyền nhân viên";

        //    if (data.IsWorking)
        //    {
        //        ModelState.AddModelError("", "Nhân viên đã nghỉ việc hoặc tài khoản bị khóa, không thể lưu quyền");
        //    }

        //    if (data.Roles == null || data.Roles.Count == 0)
        //    {
        //        ModelState.AddModelError("", "Danh sách quyền trống");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        return View("ChangeRole", data);
        //    }

        //    var selectedRoles = string.Join(",", data.Roles.Where(r => r.IsSelected).Select(r => r.RoleName));

        //    await HRDataService.ChangeEmployeeRolesAsync(data.EmployeeID, selectedRoles);

        //    return RedirectToAction("Index");
        //}

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.Title = "Đổi mật khẩu nhân viên";
            return View(model);
        }

        /// <summary>
        /// Xử lý đổi mật khẩu cho nhân viên.
        /// Gọi SecurityDataService để cập nhật mật khẩu theo email của nhân viên.
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Đổi mật khẩu nhân viên";

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
                // Đổi mật khẩu thông qua SecurityDataService theo hướng bảo mật
                await SecurityDataService.ChangeEmployeePasswordAsync(model.Email, CryptHelper.HashMD5(newPassword));
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// Hiển thị form phân quyền nhân viên.
        /// Truyền danh sách quyền hiện tại của nhân viên qua ViewBag để pre-check checkbox.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Phân quyền nhân viên";

            // Lấy RoleNames hiện tại từ DB thông qua EmployeeAccount
            // Employee model không có RoleNames, cần lấy từ bảng Employees trực tiếp
            // Tạm thời lấy qua SecurityDataService (bằng cách đọc từ DB)
            // Để đơn giản, dùng Dapper trực tiếp trong controller hoặc bổ sung method vào HRDataService
            // Ở đây dùng cách đơn giản: lấy thông qua EmployeeRepository đã có
            var currentRoles = await HRDataService.GetEmployeeRoleNamesAsync(model.EmployeeID);
            ViewBag.CurrentRoles = currentRoles;

            return View(model);
        }

        /// <summary>
        /// Lưu phân quyền mới cho nhân viên thông qua SecurityDataService.
        /// Ghi đè toàn bộ quyền cũ bằng danh sách quyền mới được chọn.
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="roleNames">Mảng tên quyền được chọn từ checkbox</param>
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, string[] roleNames)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Phân quyền nhân viên";

            try
            {
                // Ghép mảng quyền thành chuỗi phân cách bởi dấu phẩy
                // Ví dụ: ["employee", "admin"] => "employee;admin"
                string roleNamesStr = string.Join(",", roleNames ?? Array.Empty<string>());

                // Cập nhật quyền qua SecurityDataService — dùng Email làm userName
                await SecurityDataService.ChangeEmployeeRoleNamesAsync(model.Email, roleNamesStr);

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
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                bool isUsed = await HRDataService.IsUsedEmployeeAsync(id);
                if (!employee.IsWorking || isUsed)
                {
                    ViewBag.AllowDelete = false;
                    ModelState.AddModelError("", "Nhân viên này không thể xóa");
                    return View(employee);
                }

                await HRDataService.DeleteEmployeeAsync(id);
                return RedirectToAction("Index");
            }

            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            bool allowDelete = model.IsWorking
                               && !(await HRDataService.IsUsedEmployeeAsync(id));

            ViewBag.AllowDelete = allowDelete;
            return View(model);
        }
    }
}
