// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

namespace ArcaneVault.Web.Helpers
{
    /// <summary>
    /// Centralises all session key names and helper methods for reading/writing login state and JWT token.
    /// </summary>
    public static class SessionHelper
    {
        public const string KeyUserName = "UserName";
        public const string KeyEmail    = "Email";
        public const string KeyRoleId   = "RoleId";
        public const string KeyRoleName = "RoleName";
        public const string KeyJwtToken = "JwtToken";

        public static bool IsLoggedIn(ISession session)
            => !string.IsNullOrEmpty(session.GetString(KeyUserName));

        public static bool IsStaff(ISession session)
            => session.GetString(KeyRoleName) == "Staff";

        public static string? GetUserName(ISession session)
            => session.GetString(KeyUserName);

        /// <summary>
        /// Retrieves the JWT token from the session (for API calls requiring authentication).
        /// </summary>
        public static string? GetJwtToken(ISession session)
            => session.GetString(KeyJwtToken);

        public static void SetUser(ISession session,
            string userName, string email, int roleId, string roleName, string? jwtToken = null)
        {
            session.SetString(KeyUserName, userName);
            session.SetString(KeyEmail, email);
            session.SetInt32(KeyRoleId, roleId);
            session.SetString(KeyRoleName, roleName);
            
            if (!string.IsNullOrEmpty(jwtToken))
                session.SetString(KeyJwtToken, jwtToken);
        }

        public static void Clear(ISession session)
        {
            session.Remove(KeyUserName);
            session.Remove(KeyEmail);
            session.Remove(KeyRoleId);
            session.Remove(KeyRoleName);
            session.Remove(KeyJwtToken);
        }
    }
}

