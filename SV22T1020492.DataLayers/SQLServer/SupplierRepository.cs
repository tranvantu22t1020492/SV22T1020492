using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Partner;

namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến nhà cung cấp trên SQL Server
    /// </summary>
    public class SupplierRepository : BaseRepository, ISupplierRepository
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public SupplierRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Truy vấn, tìm kiếm và phân trang danh sách nhà cung cấp
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm, phân trang</param>
        /// <returns>Kết quả phân trang danh sách nhà cung cấp</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue}%");
            parameters.Add("@pageSize", input.PageSize);
            parameters.Add("@offset", input.Offset);

            var sql = @"SELECT COUNT(*) 
                        FROM Suppliers
                        WHERE SupplierName LIKE @searchValue OR ContactName LIKE @searchValue;

                        SELECT SupplierID, SupplierName, ContactName, Province, Address, Phone, Email
                        FROM Suppliers
                        WHERE SupplierName LIKE @searchValue OR ContactName LIKE @searchValue
                        ORDER BY SupplierName
                        OFFSET @offset ROWS
                        FETCH NEXT @pageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            int rowCount = await multi.ReadFirstAsync<int>();
            var data = (await multi.ReadAsync<Supplier>()).ToList();

            return new PagedResult<Supplier>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin 1 nhà cung cấp theo mã
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Thông tin nhà cung cấp hoặc null nếu không tồn tại</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT SupplierID, SupplierName, ContactName, Province, Address, Phone, Email FROM Suppliers WHERE SupplierID = @id";
            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { id });
        }

        /// <summary>
        /// Bổ sung một nhà cung cấp mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp cần bổ sung</param>
        /// <returns>Mã nhà cung cấp vừa được bổ sung</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO Suppliers (SupplierName, ContactName, Province, Address, Phone, Email)
                        VALUES (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (int)result;
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Suppliers 
                        SET SupplierName = @SupplierName,
                            ContactName  = @ContactName,
                            Province     = @Province,
                            Address      = @Address,
                            Phone        = @Phone,
                            Email        = @Email
                        WHERE SupplierID = @SupplierID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa nhà cung cấp có mã là id
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM Suppliers WHERE SupplierID = @id";
            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có mã là id có đang được sử dụng ở bảng khác không
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần kiểm tra</param>
        /// <returns>true nếu đang được sử dụng, false nếu chưa được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM Products WHERE SupplierID = @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }
        public async Task<bool> ValidateEmailAsync(string email, int supplierID = 0)
        {
            using var connection = GetConnection();
            var sql = @"SELECT COUNT(*) 
                FROM Suppliers
                WHERE Email = @Email AND SupplierID <> @SupplierID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, SupplierID = supplierID });
            return count == 0; // true nếu email chưa tồn tại, false nếu đã bị trùng
        }
    }
}