using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020492.BusinessLayers;
using SV22T1020492.Models.Catalog;
using SV22T1020492.Models.Common;

namespace SV22T1020492.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng liên quan đến quản lý mặt hàng,
    /// bao gồm cả thuộc tính (Attribute) và ảnh (Photo) của mặt hàng.
    /// </summary>
    
    public class ProductController : Controller
    {
        public const int PAGESIZE = 20;
        public const string SEARCH_PRODUCT = "SearchProduct";

        #region Product

        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả tìm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = PAGESIZE,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả phân trang
        /// </summary>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            // Xử lý logic khoảng giá
            if (input.MinPrice > 0 && input.MaxPrice <= 0)
            {
                // Nếu chỉ nhập giá thấp nhất, đặt giá cao nhất là số lớn nhất của hệ thống
                input.MaxPrice = decimal.MaxValue;
            }
            else if (input.MinPrice <= 0 && input.MaxPrice <= 0)
            {
                // Nếu cả hai đều là 0, Service sẽ hiểu là không lọc theo giá (tùy vào cách viết trong Data Layer của bạn)
                // Thông thường trong SQL bạn sẽ để: (@MinPrice = 0 OR Price >= @MinPrice)
            }

            // Thực hiện tìm kiếm (Các tiêu chí khác như CategoryID, SupplierID nếu = 0 thì Service tự hiểu là lấy tất cả)
            var result = await CatalogDataService.ListProductsAsync(input);

            // Reset lại giá Max để giao diện không hiển thị số khổng lồ
            if (input.MaxPrice == decimal.MaxValue) input.MaxPrice = 0;

            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return View(result);
        }

        /// <summary>
        /// Xem chi tiết mặt hàng (bao gồm thuộc tính và ảnh)
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        public async Task<IActionResult> Detail(int id)
        {
            ViewBag.Title = "Chi tiết Mặt hàng";
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            return View(model);
        }

        /// <summary>
        /// Bổ sung mặt hàng mới
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Mặt hàng";
            var model = new Product() { ProductID = 0, IsSelling = true };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin Mặt hàng";
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.ProductID = id;
            ViewBag.PhotoList = await CatalogDataService.ListPhotosAsync(id) ?? new List<ProductPhoto>();
            ViewBag.AttributeList = await CatalogDataService.ListAttributesAsync(id) ?? new List<ProductAttribute>();
            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu mặt hàng (bổ sung hoặc cập nhật).
        /// Xử lý upload ảnh đại diện nếu có.
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng từ form</param>
        /// <param name="uploadPhoto">File ảnh đại diện được upload (nếu có)</param>
        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung Mặt hàng" : "Cập nhật thông tin Mặt hàng";

                // 1. Kiểm tra các trường văn bản
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Vui lòng nhập tên mặt hàng");

                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Vui lòng nhập đơn vị tính");

                // 2. Kiểm tra Loại hàng và Nhà cung cấp (đã thêm từ bước trước)
                if (data.CategoryID <= 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");

                if (data.SupplierID <= 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");

                // 3. Cập nhật điều kiện: GIÁ PHẢI LỚN HƠN 0
                if (data.Price <= 0)
                    ModelState.AddModelError(nameof(data.Price), "Giá mặt hàng phải lớn hơn 0");

                // Nếu có lỗi dữ liệu đầu vào
                if (!ModelState.IsValid)
                {
                    // Đảm bảo load lại các danh sách phụ trợ để View không bị lỗi hiển thị
                    ViewBag.ProductID = data.ProductID;
                    ViewBag.PhotoList = await CatalogDataService.ListPhotosAsync(data.ProductID) ?? new List<ProductPhoto>();
                    ViewBag.AttributeList = await CatalogDataService.ListAttributesAsync(data.ProductID) ?? new List<ProductAttribute>();
                    return View("Edit", data);
                }

                // 4. Xử lý upload ảnh (Giữ nguyên logic của bạn)
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var folder = "products";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }

                    ImageSyncHelper.SyncToShop(fileName, folder);
                    data.Photo = fileName;
                }

                // Tiền xử lý dữ liệu trước khi lưu
                data.Photo ??= "";
                data.ProductDescription ??= "";

                // 5. Lưu vào database
                if (data.ProductID == 0)
                    await CatalogDataService.AddProductAsync(data);
                else
                    await CatalogDataService.UpdateProductAsync(data);

                // Reset Session tìm kiếm về trang 1 với tên sản phẩm vừa sửa/tạo
                ApplicationContext.SetSessionData(SEARCH_PRODUCT, new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = PAGESIZE,
                    SearchValue = data.ProductName,
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                });

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa mặt hàng (bao gồm thuộc tính và ảnh liên quan)
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteProductAsync(id);
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await CatalogDataService.IsUsedProductAsync(id));
            return View(model);
        }

        #endregion

        #region Product Attributes

        /// <summary>
        /// Hiển thị danh sách các thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        public async Task<IActionResult> ListAttributes(int id)
        {
            var data = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.ProductID = id;
            return View(data);
        }

        /// <summary>
        /// Bổ sung thuộc tính mới cho mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        [HttpGet]
        public IActionResult CreateAttribute(int id)
        {
            ViewBag.Title = "Bổ sung Thuộc tính Mặt hàng";
            var model = new ProductAttribute() { ProductID = id, DisplayOrder = 1 };
            return View("EditAttribute", model);
        }

        /// <summary>
        /// Cập nhật thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <param name="attributeId">Mã thuộc tính</param>
        [HttpGet]
        public async Task<IActionResult> EditAttribute(int id, long attributeId)
        {
            ViewBag.Title = "Cập nhật Thuộc tính Mặt hàng";
            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null)
                return RedirectToAction("Edit", new { id });
            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu thuộc tính (bổ sung hoặc cập nhật)
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính từ form</param>
        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            ViewBag.Title = data.AttributeID == 0 ? "Bổ sung Thuộc tính Mặt hàng" : "Cập nhật Thuộc tính Mặt hàng";

            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Vui lòng nhập tên thuộc tính");
            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Vui lòng nhập giá trị thuộc tính");
            if (data.DisplayOrder < 0)
                ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị không được âm");

            if (!ModelState.IsValid)
                return View("EditAttribute", data);

            try
            {
                if (data.AttributeID == 0)
                    await CatalogDataService.AddAttributeAsync(data);
                else
                    await CatalogDataService.UpdateAttributeAsync(data);

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("EditAttribute", data);
            }
        }

        /// <summary>
        /// Xóa thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng (dùng để redirect)</param>
        /// <param name="attributeId">Mã thuộc tính cần xóa</param>
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            await CatalogDataService.DeleteAttributeAsync(attributeId);
            return RedirectToAction("Edit", new { id });
        }

        #endregion

        #region Product Photos

        /// <summary>
        /// Hiển thị danh sách ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        public async Task<IActionResult> ListPhotos(int id)
        {
            var data = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.ProductID = id;
            return View(data);
        }

        /// <summary>
        /// Bổ sung ảnh mới cho mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        [HttpGet]
        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung Ảnh Mặt hàng";
            var model = new ProductPhoto() { ProductID = id, DisplayOrder = 1, IsHidden = false };
            return View("EditPhoto", model);
        }

        /// <summary>
        /// Cập nhật ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <param name="photoId">Mã ảnh</param>
        [HttpGet]
        public async Task<IActionResult> EditPhoto(int id, long photoId)
        {
            ViewBag.Title = "Cập nhật Ảnh Mặt hàng";
            var model = await CatalogDataService.GetPhotoAsync(photoId);
            if (model == null)
                return RedirectToAction("Edit", new { id });
            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu ảnh (bổ sung hoặc cập nhật).
        /// Xử lý upload file ảnh nếu có.
        /// </summary>
        /// <param name="data">Dữ liệu ảnh từ form</param>
        /// <param name="uploadPhoto">File ảnh được upload (nếu có)</param>
        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            ViewBag.Title = data.PhotoID == 0 ? "Bổ sung Ảnh Mặt hàng" : "Cập nhật Ảnh Mặt hàng";

            // Xử lý upload ảnh
            if (uploadPhoto != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                var folder = "products";
                var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }

                // Đồng bộ ảnh sang project Shop
                ImageSyncHelper.SyncToShop(fileName, folder);

                // TODO: Xóa ảnh cũ nếu cập nhật
                data.Photo = fileName;
            }

            if (string.IsNullOrWhiteSpace(data.Photo))
                ModelState.AddModelError(nameof(data.Photo), "Vui lòng chọn file ảnh");
            if (data.DisplayOrder < 0)
                ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị không được âm");

            if (!ModelState.IsValid)
                return View("EditPhoto", data);

            try
            {
                if (string.IsNullOrEmpty(data.Description)) data.Description = "";

                if (data.PhotoID == 0)
                    await CatalogDataService.AddPhotoAsync(data);
                else
                    await CatalogDataService.UpdatePhotoAsync(data);

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("EditPhoto", data);
            }
        }

        /// <summary>
        /// Xóa ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng (dùng để redirect)</param>
        /// <param name="photoId">Mã ảnh cần xóa</param>
        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            // TODO: Xóa file ảnh vật lý trên server trước khi xóa record trong database
            await CatalogDataService.DeletePhotoAsync(photoId);
            return RedirectToAction("Edit", new { id });
        }

        #endregion
    }
}