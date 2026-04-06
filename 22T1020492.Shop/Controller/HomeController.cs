
using Microsoft.AspNetCore.Mvc;

namespace SV22T1020492.Shop.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(int page = 1) // Thêm tham số page ở đây
        {
            ViewBag.Page = page; // Lưu lại để truyền vào Component ở View
            return View();
        }
    }
}
