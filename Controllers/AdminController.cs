using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Flappr.Models;
using Microsoft.AspNetCore.Authentication;
using Flappr.Data;
using Flappr.Dto;
using Flappr.Filters;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;


namespace Flappr.Controllers
{
    public class AdminController : Controller
    {
        private readonly FlapprContext _context;
        private readonly IConfiguration _configuration;

        //Dependency Injection (DI) ile hem IConfiguration hem DbContext alıyorum
        public AdminController(FlapprContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public bool CheckLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Nickname")))
            {
                return false;
            }

            return true;
        }

        public Guid? GetUserId(string nickname)
        {
            var user = _context.Users
                .Where(u => u.Nickname == nickname)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefault();

            return user;
        }
        public Guid? GetFlapId(Guid flapId)
        {
            var flap = _context.Flaps
                .Where(f => f.Id == flapId)
                .Select(f => (Guid?)f.Id)
                .FirstOrDefault();

            return flap;
        }

        [CustomAuthorize]
        [Route("/edit/{nickname}")]
        public IActionResult Edit(string nickname)
        {
            ViewData["Nickname"] = HttpContext.Session.GetString("Nickname");

            var checkLogin = CheckLogin();
            if (!checkLogin)
            {
                return RedirectToAction("ErrorMessage", "Interaction");
            }

            Guid? userId = GetUserId(nickname);
            if (!Guid.TryParse(HttpContext.Session.GetString("userId"), out var currentUserId) || userId != currentUserId)
            {
                TempData["AuthError"] = "Bu profili düzenleme yetkiniz yok.";
                return View("Profile" , "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                TempData["AuthError"] = "Kullanıcı bulunamadı.";
                return View("Profile", "Home");
            }

            return View(user);
        }


        [HttpPost]
        [Route("/edit/{id}")]
        public async Task<IActionResult> Edit(UserDto model)
        {
            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
            {
                ViewBag.Message = "Kullanıcı bulunamadı.";
                return View("Msg");
            }

            user.Username = model.Username;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.Password = Helper.Hash(model.Password);
            }

            if (model.ImgUrl != null && model.Image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                user.ImgUrl = $"/uploads/{fileName}";
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Profil başarıyla güncellendi.";
            return View("Msg");
        }

        [CustomAuthorize]
        [Route("/flapEdit/{flapId}")]
        public IActionResult FlapEdit(Guid flapId)
        {
            var userIdSession = HttpContext.Session.GetString("userId");
            if (!Guid.TryParse(userIdSession, out var currentUserId))
            {
                TempData["AuthError"] = "Giriş bilgileri alınamadı.";
                return RedirectToAction("ErrorMessage", "Interaction");
            }

            var flap = _context.Flaps
                 .Include(f => f.User) 
                 .FirstOrDefault(f => f.Id == flapId);

            if (flap == null)
            {
                TempData["AuthError"] = "Flap bulunamadı.";
                return RedirectToAction("Profile", "Home");
            }

            if (flap.UserId != currentUserId)
            {
                TempData["AuthError"] = "Bu flap'ı düzenleme yetkiniz yok.";
                return RedirectToAction("Profile", "Home");
            }

            var model = new FlapRequest
            {
                Id = flap.Id,
                Detail = flap.Detail,
                CreatedDate = flap.CreatedDate,
                UserUsername = flap.User.Username,
                UserNickname = flap.User.Nickname,
                UserImgUrl = flap.User.ImgUrl
            };

            return View(model);
        }

        [HttpPost]
        [Route("/flapEdit/{flapId}")]
        public async Task<IActionResult> FlapEdit(Guid flapId, string detail)
        {
            var userIdSession = HttpContext.Session.GetString("userId");
            if (!Guid.TryParse(userIdSession, out var currentUserId))
            {
                TempData["AuthError"] = "Giriş bilgileri alınamadı.";
                return RedirectToAction("ErrorMessage", "Interaction");
            }

            var flap = _context.Flaps.FirstOrDefault(f => f.Id == flapId);

            if (flap == null)
            {
                TempData["AuthError"] = "Flap bulunamadı.";
                return RedirectToAction("Profile", "Home");
            }

            if (flap.UserId != currentUserId)
            {
                TempData["AuthError"] = "Bu flap'ı düzenleme yetkiniz yok.";
                return RedirectToAction("Profile", "Home");
            }

            flap.Detail = detail;
            flap.CreatedDate = DateTime.UtcNow;
            _context.Flaps.Update(flap);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Flap başarıyla güncellendi!";
            return RedirectToAction("Profile", "Home", new { nickname = flap.User.Nickname });
        }
    }
}
