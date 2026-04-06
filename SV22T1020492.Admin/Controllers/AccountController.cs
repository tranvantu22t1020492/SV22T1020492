using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
namespace SV22T1020492.Admin.Controllers
{
    public class AccountController : Controller
    {
        #region Login / Logout

        /// <summary>
        /// Hiển thị form đăng nhập
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập thì chuyển về trang chủ
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        /// <summary>
        /// Xử lý yêu cầu đăng nhập.
        /// Kiểm tra thông tin từ CSDL (bảng Employees), mã hóa MD5 trước khi so sánh.
        /// </summary>
        /// <param name="username">Email đăng nhập</param>
        /// <param name="password">Mật khẩu (chưa mã hóa)</param>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Giữ lại username để hiển thị lại trên form khi lỗi
            ViewBag.Username = username;

            // Kiểm tra đầu vào không được để trống
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đủ thông tin");
                return View();
            }

            try
            {
                // Mã hóa mật khẩu MD5 trước khi gửi xuống CSDL
                string hashedPassword = CryptHelper.HashMD5(password);

                // Xác thực tài khoản nhân viên từ CSDL
                var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(username, hashedPassword);

                if (userAccount == null)
                {
                    ModelState.AddModelError("Error", "Email hoặc mật khẩu không đúng, hoặc tài khoản không còn hoạt động");
                    return View();
                }

                // Tạo WebUserData từ thông tin tài khoản
                var webUserData = new WebUserData()
                {
                    UserId = userAccount.UserId,
                    UserName = userAccount.UserName,
                    DisplayName = userAccount.DisplayName,
                    Email = userAccount.Email,
                    Photo = userAccount.Photo,
                    // RoleNames lưu dạng "admin,sales" hoặc "admin;sales"
                    Roles = userAccount.RoleNames
                                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(r => r.Trim())
                                    .Where(r => !string.IsNullOrEmpty(r))
                                    .ToList()
                };

                // Ghi nhận phiên đăng nhập bằng Cookie Authentication
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    webUserData.CreatePrincipal()
                );

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", $"Đã có lỗi xảy ra: {ex.Message}");
                return View();
            }
        }

        /// <summary>
        /// Đăng xuất — xóa cookie và session, chuyển về trang đăng nhập
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Xóa session trước khi đăng xuất
            HttpContext.Session.Clear();

            // Xóa cookie xác thực
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }

        #endregion

        #region Change Password

        /// <summary>
        /// Hiển thị form đổi mật khẩu cho tài khoản đang đăng nhập
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewBag.Title = "Đổi mật khẩu";
            return View();
        }

        /// <summary>
        /// Xử lý đổi mật khẩu cho tài khoản nhân viên đang đăng nhập.
        /// Kiểm tra mật khẩu cũ, mật khẩu mới và xác nhận trước khi lưu.
        /// </summary>
        /// <param name="oldPassword">Mật khẩu cũ</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            ViewBag.Title = "Đổi mật khẩu";

            // Lấy thông tin người dùng đang đăng nhập
            var userData = User.GetUserData();
            if (userData == null)
                return RedirectToAction("Login");

            // Validate đầu vào
            if (string.IsNullOrWhiteSpace(oldPassword))
                ModelState.AddModelError("oldPassword", "Vui lòng nhập mật khẩu cũ");

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
            else if (newPassword.Length < 6)
                ModelState.AddModelError("newPassword", "Mật khẩu mới phải có ít nhất 6 ký tự");

            // --- THÊM ĐIỀU KIỆN: Mật khẩu mới không được trùng mật khẩu cũ ---
            else if (newPassword == oldPassword)
                ModelState.AddModelError("newPassword", "Mật khẩu mới không được trùng với mật khẩu cũ");

            if (string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận mật khẩu mới");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
                return View();

            try
            {
                // Kiểm tra mật khẩu cũ có đúng không bằng cách xác thực lại với CSDL
                string hashedOld = CryptHelper.HashMD5(oldPassword);
                var check = await SecurityDataService.AuthorizeEmployeeAsync(userData.UserName!, hashedOld);
                if (check == null)
                {
                    ModelState.AddModelError("oldPassword", "Mật khẩu cũ không đúng");
                    return View();
                }

                // Đổi mật khẩu mới (đã mã hóa MD5)
                string hashedNew = CryptHelper.HashMD5(newPassword);
                bool result = await SecurityDataService.ChangeEmployeePasswordAsync(userData.UserName!, hashedNew);
                if (!result)
                {
                    ModelState.AddModelError(string.Empty, "Đổi mật khẩu thất bại, vui lòng thử lại");
                    return View();
                }

                // Đổi mật khẩu thành công, đăng xuất để đăng nhập lại
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        #endregion

        #region Access Denied

        /// <summary>
        /// Trang thông báo không có quyền truy cập
        /// </summary>
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            ViewBag.Title = "Không có quyền truy cập";
            return View();
        }

        #endregion
    }
}
