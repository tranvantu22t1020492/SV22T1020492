using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.DataDictionary;

namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tỉnh/thành trên SQL Server
    /// </summary>
    public class ProvinceRepository : BaseRepository, IDataDictionaryRepository<Province>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public ProvinceRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Lấy toàn bộ danh sách tỉnh/thành sắp xếp theo tên
        /// </summary>
        /// <returns>Danh sách tỉnh/thành</returns>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = GetConnection();
            var sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";
            var data = await connection.QueryAsync<Province>(sql);
            return data.ToList();
        }
    }
}