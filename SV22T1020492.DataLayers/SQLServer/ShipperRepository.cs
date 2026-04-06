using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Partner;

namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến người giao hàng trên SQL Server
    /// </summary>
    public class ShipperRepository : BaseRepository, IGenericRepository<Shipper>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public ShipperRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Truy vấn, tìm kiếm và phân trang danh sách người giao hàng
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm, phân trang</param>
        /// <returns>Kết quả phân trang danh sách người giao hàng</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue}%");
            parameters.Add("@pageSize", input.PageSize);
            parameters.Add("@offset", input.Offset);

            var sql = @"SELECT COUNT(*)
                        FROM Shippers
                        WHERE ShipperName LIKE @searchValue;

                        SELECT ShipperID, ShipperName, Phone
                        FROM Shippers
                        WHERE ShipperName LIKE @searchValue
                        ORDER BY ShipperName
                        OFFSET @offset ROWS
                        FETCH NEXT @pageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            int rowCount = await multi.ReadFirstAsync<int>();
            var data = (await multi.ReadAsync<Shipper>()).ToList();

            return new PagedResult<Shipper>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin 1 người giao hàng theo mã
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Thông tin người giao hàng hoặc null nếu không tồn tại</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT ShipperID, ShipperName, Phone FROM Shippers WHERE ShipperID = @id";
            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { id });
        }

        /// <summary>
        /// Bổ sung một người giao hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng cần bổ sung</param>
        /// <returns>Mã người giao hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO Shippers (ShipperName, Phone)
                        VALUES (@ShipperName, @Phone);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (int)result;
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Shippers
                        SET ShipperName = @ShipperName,
                            Phone       = @Phone
                        WHERE ShipperID = @ShipperID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa người giao hàng có mã là id
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM Shippers WHERE ShipperID = @id";
            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra xem người giao hàng có mã là id có đang được sử dụng ở bảng khác không
        /// </summary>
        /// <param name="id">Mã người giao hàng cần kiểm tra</param>
        /// <returns>true nếu đang được sử dụng, false nếu chưa được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM Orders WHERE ShipperID = @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }
    }
}