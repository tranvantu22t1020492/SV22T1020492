using SV22T1020492.Models.Partner;

namespace SV22T1020492.DataLayers.Interfaces
{
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        /// <summary>
        /// Kiểm tra email của nhà cung cấp có hợp lệ (chưa bị trùng)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="supplierID">
        /// Nếu là 0 => nhà cung cấp mới, 
        /// Nếu khác 0 => nhà cung cấp đang chỉnh sửa
        /// </param>
        /// <returns>true nếu email chưa tồn tại, false nếu đã bị trùng</returns>
        Task<bool> ValidateEmailAsync(string email, int supplierID = 0);
    }
}