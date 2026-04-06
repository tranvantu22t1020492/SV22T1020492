namespace SV22T1020492.BusinessLayers
{
    public static class Configuration
    {
        private static string _connectionString;
        /// <summary>
        /// Khởi tạo cấu hình Business Layer (Hàm này phải được gọi trước khi chạy ứng dụng)
        /// </summary>
        /// <param name="connectionString">Chuỗi tham số kết nối đến CSDL</param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        /// Chuỗi tham số kết nối đến CSDL
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
