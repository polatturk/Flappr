using Flappr.Data;
using Flappr.Dto;
using Flappr.Filters;
using Flappr.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Flappr.Controllers
{
    public class InteractionController : Controller
    {
        private readonly FlapprContext _context;

        public InteractionController(FlapprContext context)
        {
            _context = context;
        }

        [CustomAuthorize]
        public IActionResult Explore(){return View();}
        [CustomAuthorize]
        public IActionResult Notifications(){return View();}
        [CustomAuthorize]
        public IActionResult Messages(){return View();}
        [AllowAnonymous]
        public IActionResult ErrorMessages(){return View();}

        [HttpPost]
        public IActionResult Follows(Guid followerId, Guid followingId)
        {
            var existingFollow = _context.Follows
                .FirstOrDefault(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (existingFollow == null)
            {
                _context.Follows.Add(new Follow { FollowerId = followerId, FollowingId = followingId });
            }
            else
            {
                _context.Follows.Remove(existingFollow);
            }

            _context.SaveChanges();

            return RedirectToAction(actionName: "Profile",controllerName: "Home",routeValues: new { UserNickname = _context.Users.Find(followingId)?.Nickname });
        }

        [HttpPost]
        public IActionResult Likes(Guid flapId)
        {
            var userIdString = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Auth"); // Giriş yoksa login sayfasına yönlendir

            var userId = Guid.Parse(userIdString);

            var existingLike = _context.FlapLike
                .FirstOrDefault(l => l.FlapId == flapId && l.UserId == userId);

            var flap = _context.Flaps.FirstOrDefault(f => f.Id == flapId);
            if (flap == null) return RedirectToAction("Index", "Home");

            if (existingLike != null)
            {
                _context.FlapLike.Remove(existingLike);
                flap.LikeCount -= 1; // LikeCount'u azalt
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
                flap.LikeCount += 1; // LikeCount'u arttır
            }

            _context.SaveChanges();

            return RedirectToAction("Index", "Home"); // Veya bulunduğun sayfaya dön
        }

    }
}
