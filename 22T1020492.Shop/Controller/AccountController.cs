using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SV22T1020492.BusinessLayers;
using SV22T1020492.BusinessLayers.Shop;
using SV22T1020492.Models.Partner;
using System.Security.Cryptography;
using System.Text;


namespace SV22T1020492.Shop.Controllers
{
    public class AccountController : Controller
    {
        /// <summary>
        /// Helper mã hóa MD5
        /// </summary>
        private string ToMD5(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            using (MD5 md5 = MD5.Create())
            {
                byte[] fromData = Encoding.UTF8.GetBytes(str);
                byte[] targetData = md5.ComputeHash(fromData);
                StringBuilder byte2String = new StringBuilder();
                for (int i = 0; i < targetData.Length; i++)
                {
                    byte2String.Append(targetData[i].ToString("x2"));
                }
                return byte2String.ToString();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new AccountCustomer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(AccountCustomer model, string confirmPassword)
        {
            // 1. Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError("CustomerName", "Họ tên khách hàng không được để trống.");

            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError("Email", "Email không được để trống.");

            if (string.IsNullOrWhiteSpace(model.Phone))
                ModelState.AddModelError("Phone", "Số điện thoại không được để trống.");

            if (string.IsNullOrWhiteSpace(model.Province))
                ModelState.AddModelError("Province", "Vui lòng chọn Tỉnh/Thành phố.");

            if (string.IsNullOrWhiteSpace(model.Address))
                ModelState.AddModelError("Address", "Địa chỉ không được để trống.");

            // 2. Kiểm tra định dạng Email
            if (!string.IsNullOrEmpty(model.Email))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(model.Email);
                    if (addr.Address != model.Email) ModelState.AddModelError("Email", "Email không đúng định dạng.");
                }
                catch
                {
                    ModelState.AddModelError("Email", "Email không đúng định dạng.");
                }
            }

            // 3. Kiểm tra mật khẩu
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Mật khẩu không được để trống.");
            }
            else if (model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (model.Password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu không trùng khớp.");
            }

            // 4. Thực hiện đăng ký nếu không có lỗi nhập liệu
            if (ModelState.IsValid)
            {
                string originalPassword = model.Password;
                model.Password = ToMD5(originalPassword);

                // Service trả về -1 nếu trùng Email
                int result = CustomerAccountService.Register(model);

                if (result == -1)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác.");
                }
                else if (result > 0)
                {
                    TempData["Message"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError("", "Đăng ký thất bại. Vui lòng liên hệ quản trị viên.");
                }

                // Trả lại mật khẩu gốc để người dùng không phải nhập lại khi có lỗi server
                model.Password = originalPassword;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            // 1. Kiểm tra để báo lỗi trống cả hai trường cùng lúc
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("Email", "Vui lòng nhập Email.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Password", "Vui lòng nhập Mật khẩu.");
            }

            // Nếu có lỗi nhập liệu thì trả về View ngay
            if (!ModelState.IsValid)
            {
                return View();
            }

            // 2. Kiểm tra logic đăng nhập
            string hashedPass = ToMD5(password);
            var user = CustomerAccountService.Login(email, hashedPass);

            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
            }
            else if (user.IsLocked)
            {
                ModelState.AddModelError("", "Tài khoản của bạn hiện đang bị khóa.");
            }

            // 3. Kiểm tra lại ModelState sau khi gọi Service
            if (!ModelState.IsValid)
            {
                return View();
            }

            // --- ĐĂNG NHẬP THÀNH CÔNG ---
            HttpContext.Session.SetInt32("UserId", user.CustomerID);
            HttpContext.Session.SetString("UserDisplayName", user.ContactName ?? "Khách hàng");
            HttpContext.Session.SetString("UserName", user.CustomerName ?? "");

            // Đợi lưu Session xong mới chuyển trang
            await HttpContext.Session.CommitAsync();

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            int? customerId = HttpContext.Session.GetInt32("UserId");
            if (customerId == null) return RedirectToAction("Login");

            var data = CustomerAccountService.GetCustomer(customerId.Value);
            if (data == null) return NotFound();

            return View(data);
        }

        // Hiển thị trang đổi mật khẩu
        [HttpGet]
        public IActionResult ChangePassword()
        {
            int? customerId = HttpContext.Session.GetInt32("UserId");
            if (customerId == null) return RedirectToAction("Login");

            return View();
        }

        // Xử lý đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            int? customerId = HttpContext.Session.GetInt32("UserId");
            if (customerId == null) return RedirectToAction("Login");

            // 1. Kiểm tra các trường không được để trống
            if (string.IsNullOrWhiteSpace(oldPassword))
                ModelState.AddModelError("oldPassword", "Vui lòng nhập mật khẩu hiện tại.");

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới.");

            if (string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận mật khẩu mới.");

            // 2. Kiểm tra độ dài mật khẩu mới (giống lúc đăng ký)
            if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < 6)
            {
                ModelState.AddModelError("newPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            // 3. Kiểm tra mật khẩu mới và xác nhận mật khẩu có khớp nhau không
            if (!string.IsNullOrEmpty(newPassword) && newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu mới không khớp.");
            }

            // 4. Kiểm tra mật khẩu mới không được trùng với mật khẩu cũ (tùy chọn nhưng nên có)
            if (!string.IsNullOrEmpty(oldPassword) && oldPassword == newPassword)
            {
                ModelState.AddModelError("newPassword", "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
            }

            if (ModelState.IsValid)
            {
                string hashedOld = ToMD5(oldPassword);
                string hashedNew = ToMD5(newPassword);

                // Gọi Service xử lý dưới Database
                bool result = CustomerAccountService.ChangePassword(customerId.Value, hashedOld, hashedNew);

                if (result)
                {
                    TempData["Message"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    // Thường là do mật khẩu cũ nhập sai
                    ModelState.AddModelError("oldPassword", "Mật khẩu hiện tại không chính xác.");
                }
            }

            // Nếu có lỗi thì ở lại trang để hiện thông báo
            return View();
        }
        [HttpGet]
        public IActionResult EditProfile()
        {
            int? customerId = HttpContext.Session.GetInt32("UserId");
            if (customerId == null) return RedirectToAction("Login");

            var data = CustomerAccountService.GetCustomer(customerId.Value);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(AccountCustomer model)
        {
            if (ModelState.IsValid)
            {
                bool result = CustomerAccountService.UpdateCustomer(model);
                if (result)
                {
                    // Cập nhật lại tên hiển thị trên Header nếu người dùng đổi tên
                    HttpContext.Session.SetString("UserDisplayName", model.ContactName ?? "");

                    TempData["Message"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }
                ModelState.AddModelError("", "Cập nhật thất bại. Vui lòng thử lại.");
            }
            return View(model);
        }
    }
}