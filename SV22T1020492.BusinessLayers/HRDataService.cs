using SV22T1020492.BusinessLayers;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.DataLayers.SQLServer;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.HR;

namespace SV22T1020492.BusinessLayers
{
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            return await employeeDB.GetAsync(employeeID);
        }

        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            return await employeeDB.AddAsync(data);
        }

        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            return await employeeDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            if (await employeeDB.IsUsedAsync(employeeID))
                return false;

            return await employeeDB.DeleteAsync(employeeID);
        }

        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            return await employeeDB.IsUsedAsync(employeeID);
        }

        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
        {
            return await employeeDB.ValidateEmailAsync(email, employeeID);
        }

        #endregion

        #region Role

        // 👉 CHỈ GỌI XUỐNG REPOSITORY (KHÔNG VIẾT SQL Ở ĐÂY)
        public static async Task<List<string>> GetEmployeeRoleNamesAsync(int employeeID)
        {
            return await employeeDB.GetRoleNamesAsync(employeeID);
        }

        #endregion
    }
}