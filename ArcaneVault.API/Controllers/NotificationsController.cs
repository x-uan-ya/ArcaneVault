// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;

        public NotificationsController(ArcaneVaultDbContext db) => _db = db;

        /// <summary>GET /api/notifications — all notifications for the current user, newest first.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var notifications = await _db.Notifications
                .Where(n => n.UserName == username)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Message,
                    n.Category,
                    n.LinkUrl,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            var unreadCount = await _db.Notifications
                .CountAsync(n => n.UserName == username && !n.IsRead);

            return Ok(new { notifications, unreadCount });
        }

        /// <summary>PUT /api/notifications/{id}/read — mark one notification as read.</summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var username = User.Identity?.Name;
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserName == username);

            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>PUT /api/notifications/read-all — mark all as read.</summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var unread = await _db.Notifications
                .Where(n => n.UserName == username && !n.IsRead)
                .ToListAsync();

            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>DELETE /api/notifications — clear all notifications for the user.</summary>
        [HttpDelete]
        public async Task<IActionResult> ClearAll()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var all = await _db.Notifications
                .Where(n => n.UserName == username)
                .ToListAsync();

            _db.Notifications.RemoveRange(all);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
