using SV22T1020492.DataLayers.SQLServer;
using SV22T1020492.Models.Partner;

namespace SV22T1020492.BusinessLayers.Shop
{
    public static class CustomerAccountService
    {
        /// <summary>
        /// Đăng ký tài khoản khách hàng mới
        /// </summary>
        public static int Register(AccountCustomer data)
        {
            return CustomerAccountDAL.Register(data);
        }

        /// <summary>
        /// Kiểm tra đăng nhập
        /// </summary>
        public static AccountCustomer? Login(string email, string password)
        {
            return CustomerAccountDAL.Login(email, password);
        }

        /// <summary>
        /// Lấy thông tin chi tiết khách hàng (Sửa lỗi return null)
        /// </summary>
        public static AccountCustomer? GetCustomer(int customerId)
        {
            return CustomerAccountDAL.GetCustomer(customerId);
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản (Đã khớp 3 tham số với DAL)
        /// </summary>
        public static bool ChangePassword(int customerId, string oldPassword, string newPassword)
        {
            return CustomerAccountDAL.ChangePassword(customerId, oldPassword, newPassword);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng (Nếu bạn có viết hàm Update ở DAL)
        /// </summary>
        public static bool UpdateCustomer(AccountCustomer data)
        {
            return CustomerAccountDAL.Update(data);
        }
    }
}