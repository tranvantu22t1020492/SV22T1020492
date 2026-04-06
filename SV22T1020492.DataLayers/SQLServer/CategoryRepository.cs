using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Catalog;
using SV22T1020492.Models.Common;

namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến loại hàng trên SQL Server
    /// </summary>
    public class CategoryRepository : BaseRepository, IGenericRepository<Category>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public CategoryRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Truy vấn, tìm kiếm và phân trang danh sách loại hàng
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm, phân trang</param>
        /// <returns>Kết quả phân trang danh sách loại hàng</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue}%");
            parameters.Add("@pageSize", input.PageSize);
            parameters.Add("@offset", input.Offset);

            var sql = @"SELECT COUNT(*) 
                        FROM Categories
                        WHERE CategoryName LIKE @searchValue;

                        SELECT CategoryID, CategoryName, Description
                        FROM Categories
                        WHERE CategoryName LIKE @searchValue
                        ORDER BY CategoryName
                        OFFSET @offset ROWS
                        FETCH NEXT @pageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            int rowCount = await multi.ReadFirstAsync<int>();
            var data = (await multi.ReadAsync<Category>()).ToList();

            return new PagedResult<Category>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin 1 loại hàng theo mã
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Thông tin loại hàng hoặc null nếu không tồn tại</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT CategoryID, CategoryName, Description FROM Categories WHERE CategoryID = @id";
            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { id });
        }

        /// <summary>
        /// Bổ sung một loại hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng cần bổ sung</param>
        /// <returns>Mã loại hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO Categories (CategoryName, Description)
                        VALUES (@CategoryName, @Description);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (int)result;
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Categories 
                        SET CategoryName = @CategoryName,
                            Description  = @Description
                        WHERE CategoryID = @CategoryID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa loại hàng có mã là id
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM Categories WHERE CategoryID = @id";
            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra xem loại hàng có mã là id có được sử dụng ở bảng khác không
        /// </summary>
        /// <param name="id">Mã loại hàng cần kiểm tra</param>
        /// <returns>true nếu đang được sử dụng, false nếu chưa được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM Products WHERE CategoryID = @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }
    }
}