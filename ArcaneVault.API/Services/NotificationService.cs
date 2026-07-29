// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;

namespace ArcaneVault.API.Services
{
    public interface INotificationService
    {
        Task SendAsync(string userName, string message, string category = "system", string? linkUrl = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ArcaneVaultDbContext _db;

        public NotificationService(ArcaneVaultDbContext db) => _db = db;

        public async Task SendAsync(string userName, string message, string category = "system", string? linkUrl = null)
        {
            _db.Notifications.Add(new Notification
            {
                UserName  = userName,
                Message   = message,
                Category  = category,
                LinkUrl   = linkUrl,
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
