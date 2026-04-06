using Microsoft.Data.SqlClient;

namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp cơ sở cho các lớp cài đặt các phép xử lý dữ liệu
    /// trên CSDL SQL Server
    /// </summary>
    public abstract class BaseRepository
    {
        protected string _connectionString;
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString"></param>
        public BaseRepository(string connectionString)
        {
            _connectionString  = connectionString;
        }

        /// <summary>
        /// Lấy đối tượng kết nối đến CSDLss
        /// </summary>
        /// <returns></returns>
        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
