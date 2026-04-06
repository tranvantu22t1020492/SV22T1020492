namespace SV22T1020492.Admin
{
    /// <summary>
    /// Tiện ích đồng bộ ảnh giữa Admin và Shop.
    /// Khi upload ảnh ở Admin, tự động copy sang wwwroot tương ứng của Shop.
    /// </summary>
    public static class ImageSyncHelper
    {
        /// <summary>
        /// Copy file ảnh vừa upload sang thư mục tương ứng của project Shop.
        /// </summary>
        /// <param name="fileName">Tên file ảnh (vd: abc123.jpg)</param>
        /// <param name="subFolder">Thư mục con: "employees" hoặc "products"</param>
        public static void SyncToShop(string fileName, string subFolder)
        {
            try
            {
                string shopWwwRoot = ApplicationContext.GetConfigValue("ShopWwwRoot");

                if (string.IsNullOrEmpty(shopWwwRoot))
                {
                    string contentRoot = ApplicationContext.ApplicationRootPath;
                    string solutionDir = Path.GetFullPath(Path.Combine(contentRoot, ".."));
                    // Sửa lại đúng mã SV của Trung ở đây
                    shopWwwRoot = Path.Combine(solutionDir, "SV22T1020779.Shop", "wwwroot");
                }

                string destDir = Path.Combine(shopWwwRoot, "images", subFolder);

                // TỰ ĐỘNG TẠO THƯ MỤC NẾU CHƯA CÓ
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                string destFile = Path.Combine(destDir, fileName);
                string srcFile = Path.Combine(ApplicationContext.WWWRootPath, "images", subFolder, fileName);

                if (File.Exists(srcFile))
                {
                    // Copy đè file sang Shop
                    File.Copy(srcFile, destFile, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                // Ghi log để debug nếu cần: System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}