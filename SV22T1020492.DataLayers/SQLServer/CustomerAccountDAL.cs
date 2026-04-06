using System.Data;
using Microsoft.Data.SqlClient;
using SV22T1020492.Models.Partner;

namespace SV22T1020492.DataLayers.SQLServer
{
    public static class CustomerAccountDAL
    {
        // Chuỗi kết nối của bạn
        private static string connectionString = @"Server=.\SQLEXPRESS;Database=LiteCommerceDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static int Register(AccountCustomer data)
        {
            int id = 0;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE Email = @Email", connection);
                checkCmd.Parameters.AddWithValue("@Email", data.Email ?? "");
                if ((int)checkCmd.ExecuteScalar() > 0) return -1;

                var sql = @"INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                            VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, 0);
                            SELECT SCOPE_IDENTITY();";

                var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@CustomerName", data.CustomerName ?? "");
                cmd.Parameters.AddWithValue("@ContactName", data.ContactName ?? "");
                cmd.Parameters.AddWithValue("@Province", data.Province ?? "");
                cmd.Parameters.AddWithValue("@Address", data.Address ?? "");
                cmd.Parameters.AddWithValue("@Phone", data.Phone ?? "");
                cmd.Parameters.AddWithValue("@Email", data.Email ?? "");
                cmd.Parameters.AddWithValue("@Password", data.Password ?? "");

                id = Convert.ToInt32(cmd.ExecuteScalar());
                connection.Close();
            }
            return id;
        }

        public static AccountCustomer? Login(string email, string password)
        {
            AccountCustomer? user = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sql = "SELECT * FROM Customers WHERE Email = @Email AND Password = @Password";
                var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (reader.Read())
                    {
                        user = new AccountCustomer
                        {
                            CustomerID = Convert.ToInt32(reader["CustomerID"]),
                            CustomerName = reader["CustomerName"].ToString(),
                            ContactName = reader["ContactName"].ToString(),
                            Email = reader["Email"].ToString(),
                            IsLocked = Convert.ToBoolean(reader["IsLocked"])
                        };
                    }
                }
            }
            return user;
        }

        // --- BỔ SUNG HÀM LẤY THÔNG TIN CHI TIẾT ĐỂ HIỆN PROFILE ---
        public static AccountCustomer? GetCustomer(int customerId)
        {
            AccountCustomer? data = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sql = "SELECT * FROM Customers WHERE CustomerID = @CustomerID";
                var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@CustomerID", customerId);

                using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (reader.Read())
                    {
                        data = new AccountCustomer
                        {
                            CustomerID = Convert.ToInt32(reader["CustomerID"]),
                            CustomerName = reader["CustomerName"]?.ToString() ?? "",
                            ContactName = reader["ContactName"]?.ToString() ?? "",
                            Province = reader["Province"]?.ToString() ?? "",
                            Address = reader["Address"]?.ToString() ?? "",
                            Phone = reader["Phone"]?.ToString() ?? "",
                            Email = reader["Email"]?.ToString() ?? "",
                            IsLocked = Convert.ToBoolean(reader["IsLocked"])
                        };
                    }
                }
            }
            return data;
        }

        // --- BỔ SUNG HÀM ĐỔI MẬT KHẨU (SỬA LỖI 3 THAM SỐ) ---
        public static bool ChangePassword(int customerId, string oldPassword, string newPassword)
        {
            bool result = false;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Chỉ Update khi đúng ID và đúng mật khẩu cũ
                var sql = @"UPDATE Customers 
                            SET Password = @NewPassword 
                            WHERE CustomerID = @CustomerID AND Password = @OldPassword";

                var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                cmd.Parameters.AddWithValue("@OldPassword", oldPassword);
                cmd.Parameters.AddWithValue("@NewPassword", newPassword);

                result = cmd.ExecuteNonQuery() > 0;
                connection.Close();
            }
            return result;
        }
        public static bool Update(AccountCustomer data)
        {
            bool result = false;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sql = @"UPDATE Customers 
                       SET CustomerName = @CustomerName, 
                           ContactName = @ContactName, 
                           Province = @Province, 
                           Address = @Address, 
                           Phone = @Phone
                       WHERE CustomerID = @CustomerID";
                var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@CustomerName", data.CustomerName ?? "");
                cmd.Parameters.AddWithValue("@ContactName", data.ContactName ?? "");
                cmd.Parameters.AddWithValue("@Province", data.Province ?? "");
                cmd.Parameters.AddWithValue("@Address", data.Address ?? "");
                cmd.Parameters.AddWithValue("@Phone", data.Phone ?? "");
                cmd.Parameters.AddWithValue("@CustomerID", data.CustomerID);

                result = cmd.ExecuteNonQuery() > 0;
                connection.Close();
            }
            return result;
        }
    }
}