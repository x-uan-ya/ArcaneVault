// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

namespace ArcaneVault.Web.Helpers
{
    /// <summary>
    /// Extension methods for HttpClient to simplify JWT token management.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Sets the Authorization header with the JWT bearer token from the session.
        /// </summary>
        public static void SetAuthorizationToken(this HttpClient client, ISession session)
        {
            var token = SessionHelper.GetJwtToken(session);
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}
