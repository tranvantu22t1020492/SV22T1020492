using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Security;

namespace SV22T1020492.DataLayers.SQLServer
{
    public class EmployeeAccountRepository : BaseRepository, IUserAccountRepository
    {
        public EmployeeAccountRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = @"SELECT CAST(EmployeeID AS nvarchar(20)) AS UserId,
                               Email        AS UserName,
                               FullName     AS DisplayName,
                               Email,
                               ISNULL(Photo, N'') AS Photo,
                               ISNULL(RoleNames, N'') AS RoleNames
                        FROM   Employees
                        WHERE  Email     = @userName
                          AND  Password  = @password
                          AND  IsWorking = 1";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = "UPDATE Employees SET Password = @password WHERE Email = @userName";

            int rows = await connection.ExecuteAsync(sql, new { userName, password });
            return rows > 0;
        }

        public async Task<bool> ChangeRoleNamesAsync(string userName, string roleNames)
        {
            using var connection = GetConnection();
            var sql = "UPDATE Employees SET RoleNames = @roleNames WHERE Email = @userName";

            int rows = await connection.ExecuteAsync(sql, new { userName, roleNames });
            return rows > 0;
        }
    }
}