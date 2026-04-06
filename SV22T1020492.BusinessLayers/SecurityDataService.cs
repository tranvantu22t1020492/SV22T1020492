using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.DataLayers.SQLServer;
using SV22T1020492.Models.Security;

namespace SV22T1020492.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bảo mật,
    /// bao gồm xác thực tài khoản, đổi mật khẩu và phân quyền
    /// cho nhân viên (Admin) và khách hàng (Shop).
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository employeeAccountDB;
        private static readonly IUserAccountRepository customerAccountDB;

        /// <summary>
        /// Constructor — khởi tạo repository cho nhân viên và khách hàng
        /// </summary>
        static SecurityDataService()
        {
            employeeAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);
            customerAccountDB = new CustomerAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập của nhân viên (dùng cho Admin).
        /// </summary>
        /// <param name="userName">Email đăng nhập của nhân viên</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>
        /// Thông tin tài khoản nếu đăng nhập hợp lệ, ngược lại trả về null
        /// </returns>
        public static async Task<UserAccount?> AuthorizeEmployeeAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;
            return await employeeAccountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập của khách hàng (dùng cho Shop).
        /// </summary>
        /// <param name="userName">Email đăng nhập của khách hàng</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>
        /// Thông tin tài khoản nếu đăng nhập hợp lệ, ngược lại trả về null
        /// </returns>
        public static async Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;
            return await customerAccountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu cho tài khoản nhân viên.
        /// </summary>
        /// <param name="userName">Email của nhân viên</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>true nếu đổi mật khẩu thành công</returns>
        public static async Task<bool> ChangeEmployeePasswordAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Tên đăng nhập và mật khẩu không được để trống.");
            return await employeeAccountDB.ChangePasswordAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu cho tài khoản khách hàng.
        /// </summary>
        /// <param name="userName">Email của khách hàng</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>true nếu đổi mật khẩu thành công</returns>
        public static async Task<bool> ChangeCustomerPasswordAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Tên đăng nhập và mật khẩu không được để trống.");
            return await customerAccountDB.ChangePasswordAsync(userName, password);
        }

        /// <summary>
        /// Cập nhật danh sách quyền (RoleNames) cho tài khoản nhân viên.
        /// Danh sách quyền được lưu dưới dạng chuỗi phân cách bởi dấu chấm phẩy
        /// trong cột RoleNames của bảng Employees.
        /// </summary>
        /// <param name="userName">Email của nhân viên cần cập nhật quyền</param>
        /// <param name="roleNames">
        /// Chuỗi tên các quyền phân cách bởi dấu chấm phẩy,
        /// ví dụ: "Employees;Orders;Products".
        /// Truyền chuỗi rỗng để xóa toàn bộ quyền.
        /// </param>
        /// <returns>true nếu cập nhật thành công</returns>
        public static async Task<bool> ChangeEmployeeRoleNamesAsync(string userName, string roleNames)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Tên đăng nhập không được để trống.");
            // roleNames có thể rỗng (xóa toàn bộ quyền) — không throw exception
            return await employeeAccountDB.ChangeRoleNamesAsync(userName, roleNames ?? "");
        }
    }
}