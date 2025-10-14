using Flappr.Data;
using Flappr.Dto;
using Flappr.Filters;
using Flappr.Hubs;
using Flappr.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Flappr.Controllers
{
    public class InteractionController : Controller
    {
        private readonly FlapprContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public InteractionController(FlapprContext context,IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;

        }

        [CustomAuthorize]
        public async Task<IActionResult> Notifications()
        {
            var userIdString = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Home");

            var userNotifications = await _context.Notifications
            .Where(n => n.UserId == userIdString)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();

            ViewBag.UnreadCount = userNotifications.Count(n => !n.IsRead);


            foreach (var n in userNotifications.Where(n => !n.IsRead))
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();

            return View(userNotifications);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteNotifications()
        {
            var userIdString = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Home");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userIdString)
                .ToListAsync();

            if (notifications.Any())
            {
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Notifications"); 
        }

        [AllowAnonymous]
        public IActionResult ErrorMessages(){return View();}
        public IActionResult ServiceUnavailable()
        {
            Response.StatusCode = 503;
            return View();
        }
        public IActionResult Contact() 
        {
            return View(); 
        }

        [HttpPost]
        public async Task<IActionResult> Follows(Guid followerId, Guid followingId)
        {
            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (existingFollow == null)
            {
                _context.Follows.Add(new Follow { FollowerId = followerId, FollowingId = followingId });
                await _context.SaveChangesAsync();

                var followerUser = await _context.Users.FindAsync(followerId);
                var notification = new Notification
                {
                    UserId = followingId.ToString(),          
                    SenderId = followerId.ToString(),         
                    Type = "Yeni takipçi 🎉",
                    Message = $"{followerUser.Nickname}, seni takip etmeye başladı.",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(followingId.ToString())
                    .SendAsync("ReceiveNotification", notification.Message);
            }
            else
            {
                _context.Follows.Remove(existingFollow);
                await _context.SaveChangesAsync();
            }

            var followingUser = await _context.Users.FindAsync(followingId);
            return RedirectToAction(
                actionName: "Profile",
                controllerName: "Home",
                routeValues: new { UserNickname = followingUser?.Nickname }
            );
        }

        [HttpPost]
        public async Task<IActionResult> Likes(Guid flapId, string returnUrl)
        {
            var userIdString = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Home");       

            var userId = Guid.Parse(userIdString);

            var existingLike = await _context.FlapLike
                .FirstOrDefaultAsync(l => l.FlapId == flapId && l.UserId == userId);

            var flap = await _context.Flaps
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == flapId);

            if (flap == null) return RedirectToAction("Index", "Home");

            if (existingLike != null)
            {
                _context.FlapLike.Remove(existingLike);
                flap.LikeCount -= 1;
            }
            else
            {
                var newLike = new FlapLike
                {
                    Id = Guid.NewGuid(),
                    FlapId = flapId,
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.FlapLike.Add(newLike);
                flap.LikeCount += 1;

                if (flap.UserId != userId)
                {
                    var likerUser = await _context.Users.FindAsync(userId);

                    var notification = new Notification
                    {
                        UserId = flap.UserId.ToString(),     
                        SenderId = userId.ToString(),        
                        Type = "Yeni beğeni 🎉",
                        Message = $"{likerUser?.Nickname ?? likerUser?.Username}, paylaştığın gönderiyi beğendi.",
                        CreatedDate = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.User(flap.UserId.ToString())
                        .SendAsync("ReceiveNotification", notification.Message);
                }
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

    }
}
