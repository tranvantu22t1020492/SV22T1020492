using SV22T1020492.Models.Security;

namespace SV22T1020492.DataLayers.Interfaces
{
    public interface IUserAccountRepository
    {
        Task<UserAccount?> AuthorizeAsync(string userName, string password);

        Task<bool> ChangePasswordAsync(string userName, string password);

        Task<bool> ChangeRoleNamesAsync(string userName, string roleNames);
    }
}