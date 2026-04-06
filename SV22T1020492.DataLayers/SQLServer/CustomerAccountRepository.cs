using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Security;

namespace SV22T1020492.DataLayers.SQLServer
{
    public class CustomerAccountRepository : BaseRepository, IUserAccountRepository
    {
        public CustomerAccountRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = @"SELECT CAST(CustomerID AS nvarchar(20)) AS UserId,
                               Email         AS UserName,
                               CustomerName  AS DisplayName,
                               Email,
                               N''           AS Photo,
                               N''           AS RoleNames
                        FROM   Customers
                        WHERE  Email     = @userName
                          AND  Password  = @password
                          AND  IsLocked  = 0";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = "UPDATE Customers SET Password = @password WHERE Email = @userName";

            int rows = await connection.ExecuteAsync(sql, new { userName, password });
            return rows > 0;
        }

        public Task<bool> ChangeRoleNamesAsync(string userName, string roleNames)
        {
            // Customer không cần role → return false
            return Task.FromResult(false);
        }
    }
}