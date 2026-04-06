using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.HR;

namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến nhân viên trên SQL Server
    /// </summary>
    public class EmployeeRepository : BaseRepository, IEmployeeRepository
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public EmployeeRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Truy vấn, tìm kiếm và phân trang danh sách nhân viên
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm, phân trang</param>
        /// <returns>Kết quả phân trang danh sách nhân viên</returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue}%");
            parameters.Add("@pageSize", input.PageSize);
            parameters.Add("@offset", input.Offset);

            var sql = @"SELECT COUNT(*)
                        FROM Employees
                        WHERE FullName LIKE @searchValue;

                        SELECT EmployeeID, FullName, BirthDate, Address, Phone, Email, Photo, IsWorking
                        FROM Employees
                        WHERE FullName LIKE @searchValue
                        ORDER BY FullName
                        OFFSET @offset ROWS
                        FETCH NEXT @pageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            int rowCount = await multi.ReadFirstAsync<int>();
            var data = (await multi.ReadAsync<Employee>()).ToList();

            return new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin 1 nhân viên theo mã
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Thông tin nhân viên hoặc null nếu không tồn tại</returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT EmployeeID, FullName, BirthDate, Address, Phone, Email, Photo, IsWorking FROM Employees WHERE EmployeeID = @id";
            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { id });
        }

        /// <summary>
        /// Bổ sung một nhân viên mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên cần bổ sung</param>
        /// <returns>Mã nhân viên vừa được bổ sung</returns>
        public async Task<int> AddAsync(Employee data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO Employees (FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking, RoleNames)
                        VALUES (@FullName, @BirthDate, @Address, @Phone, @Email, '', @Photo, @IsWorking, '');
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (int)result;
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Employees
                        SET FullName   = @FullName,
                            BirthDate  = @BirthDate,
                            Address    = @Address,
                            Phone      = @Phone,
                            Email      = @Email,
                            Photo      = @Photo,
                            IsWorking  = @IsWorking
                        WHERE EmployeeID = @EmployeeID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa nhân viên có mã là id
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM Employees WHERE EmployeeID = @id";
            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra xem nhân viên có mã là id có đang được sử dụng ở bảng khác không
        /// </summary>
        /// <param name="id">Mã nhân viên cần kiểm tra</param>
        /// <returns>true nếu đang được sử dụng, false nếu chưa được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM Orders WHERE EmployeeID = @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ (không bị trùng) hay không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới.
        /// Nếu id &lt;&gt; 0: Kiểm tra email của nhân viên có EmployeeID = id (bỏ qua bản ghi hiện tại)
        /// </param>
        /// <returns>true nếu email hợp lệ (chưa bị trùng), false nếu email đã tồn tại</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = GetConnection();
            var sql = "SELECT COUNT(*) FROM Employees WHERE Email = @email AND EmployeeID <> @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });
            return count == 0;
        }
        public async Task<List<string>> GetRoleNamesAsync(int employeeID)
        {
            using var connection = GetConnection();

            var sql = @"SELECT ISNULL(RoleNames, '') 
                FROM Employees 
                WHERE EmployeeID = @employeeID";

            var roleNames = await connection.QueryFirstOrDefaultAsync<string>(sql, new { employeeID });

            if (string.IsNullOrWhiteSpace(roleNames))
                return new List<string>();

            return roleNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(r => r.Trim())
                            .ToList();
        }
    }
}