using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Partner;


namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến khách hàng trên SQL Server
    /// </summary>
    public class CustomerRepository : BaseRepository, ICustomerRepository
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public CustomerRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Truy vấn, tìm kiếm và phân trang danh sách khách hàng
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm, phân trang</param>
        /// <returns>Kết quả phân trang danh sách khách hàng</returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue}%");
            parameters.Add("@pageSize", input.PageSize);
            parameters.Add("@offset", input.Offset);

            var sql = @"SELECT COUNT(*)
                        FROM Customers
                        WHERE CustomerName LIKE @searchValue OR ContactName LIKE @searchValue;

                        SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked
                        FROM Customers
                        WHERE CustomerName LIKE @searchValue OR ContactName LIKE @searchValue
                        ORDER BY CustomerName
                        OFFSET @offset ROWS
                        FETCH NEXT @pageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            int rowCount = await multi.ReadFirstAsync<int>();
            var data = (await multi.ReadAsync<Customer>()).ToList();

            return new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin 1 khách hàng theo mã
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>Thông tin khách hàng hoặc null nếu không tồn tại</returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked FROM Customers WHERE CustomerID = @id";
            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { id });
        }

        /// <summary>
        /// Bổ sung một khách hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng cần bổ sung</param>
        /// <returns>Mã khách hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO Customers (CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                        VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, '', @IsLocked);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (int)result;
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Customers 
                        SET CustomerName = @CustomerName,
                            ContactName  = @ContactName,
                            Province     = @Province,
                            Address      = @Address,
                            Phone        = @Phone,
                            Email        = @Email,
                            IsLocked     = @IsLocked
                        WHERE CustomerID = @CustomerID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa khách hàng có mã là id
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM Customers WHERE CustomerID = @id";
            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra xem khách hàng có mã là id có đang được sử dụng ở bảng khác không
        /// </summary>
        /// <param name="id">Mã khách hàng cần kiểm tra</param>
        /// <returns>true nếu đang được sử dụng, false nếu chưa được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM Orders WHERE CustomerID = @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        /// <summary>
        /// Kiểm tra xem một địa chỉ email của khách hàng có hợp lệ (không bị trùng) hay không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của khách hàng mới.
        /// Nếu id &lt;&gt; 0: Kiểm tra email đối với khách hàng đã tồn tại (bỏ qua bản ghi có CustomerID = id)
        /// </param>
        /// <returns>true nếu email hợp lệ (chưa bị trùng), false nếu email đã tồn tại</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM Customers WHERE Email = @email AND CustomerID <> @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });
            return count == 0;
        }
    }
}