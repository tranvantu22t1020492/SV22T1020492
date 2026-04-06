using System.Security.Cryptography;
using System.Text;

namespace SV22T1020492.BusinessLayers // Đổi lại đúng Namespace của bạn
{
    public static class SecurityService
    {
        public static string ToMD5(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                // Dùng Encoding.UTF8 sẽ tốt hơn ASCII nếu mật khẩu có ký tự đặc biệt
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}