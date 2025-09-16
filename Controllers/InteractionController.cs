using Flappr.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flappr.Controllers
{
    public class InteractionController : Controller
    {
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
    }
}
