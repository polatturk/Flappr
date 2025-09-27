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
        public IActionResult Explore()
        {
            return View();
        }
        [CustomAuthorize]
        public IActionResult Notifications()
        {
            return View();
        }
        [CustomAuthorize]
        public IActionResult Messages()
        {
            return View();
        }
        [AllowAnonymous]
        public IActionResult ErrorMessages()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Follows(int followerId, int followingId)
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

            return RedirectToAction(
                actionName: "Profile",
                controllerName: "Home",
                routeValues: new { UserNickname = _context.Users.Find(followingId)?.Nickname }
            );
        }


    }
}
