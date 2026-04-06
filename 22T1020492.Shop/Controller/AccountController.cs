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
            // 1. Kiểm tra mật khẩu khớp
            if (model.Password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu không khớp.");
            }

            if (ModelState.IsValid)
            {
                // 2. Mã hóa mật khẩu trước khi gửi xuống Service
                // LƯU Ý: Nếu trong CustomerAccountService.Register đã có MD5 thì hãy comment dòng dưới lại.
                model.Password = ToMD5(model.Password);

                // 3. Gọi Service lưu vào DB
                int result = CustomerAccountService.Register(model);

                if (result == -1)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác.");
                    return View(model);
                }

                if (result > 0)
                {
                    TempData["Message"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", "Đăng ký thất bại do lỗi hệ thống (DAL chưa thực hiện lưu).");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password) // Thêm async Task vào đây
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ Email và Mật khẩu.");
                return View();
            }

            // Mã hóa mật khẩu người dùng nhập để so sánh với mật khẩu đã mã hóa trong DB
            string hashedPass = ToMD5(password);

            // Gọi Service Login
            var user = CustomerAccountService.Login(email, hashedPass);

            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View();
            }

            if (user.IsLocked)
            {
                ModelState.AddModelError("", "Tài khoản của bạn hiện đang bị khóa.");
                return View();
            }

            // --- LƯU SESSION ---
            HttpContext.Session.SetInt32("UserId", user.CustomerID);
            HttpContext.Session.SetString("UserDisplayName", user.ContactName ?? "Khách hàng");
            HttpContext.Session.SetString("UserName", user.CustomerName ?? "");

            // Đợi lưu Session xong mới chuyển trang để Header cập nhật kịp số lượng giỏ hàng
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

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu mới không khớp.");
            }

            if (ModelState.IsValid)
            {
                string hashedOld = ToMD5(oldPassword);
                string hashedNew = ToMD5(newPassword);

                // Gọi Service (nhớ cập nhật DAL như hướng dẫn trước đó)
                bool result = CustomerAccountService.ChangePassword(customerId.Value, hashedOld, hashedNew);

                if (result)
                {
                    TempData["Message"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile"); // Thành công thì quay về Profile để xem thông báo
                }
                else
                {
                    ModelState.AddModelError("", "Mật khẩu hiện tại không chính xác.");
                }
            }

            return View(); // Nếu có lỗi thì ở lại trang ChangePassword để hiện thông báo lỗi
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