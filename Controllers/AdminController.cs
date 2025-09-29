using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Flappr.Models;
using Microsoft.AspNetCore.Authentication;
using Flappr.Data;
using Flappr.Dto;

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

        public bool CheckLoginn()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("nickname")))
            {
                return false;
            }

            return true;
        }

        public Guid? UserIdGetirr(string nickname)
        {
            var user = _context.Users
                .Where(u => u.Nickname == nickname)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefault();

            return user;
        }

        [Route("/duzenle/{nickname}")]
        public IActionResult Duzenle(string nickname)
        {
            ViewData["Nickname"] = HttpContext.Session.GetString("Nickname");

            var checkLogin = CheckLoginn();
            if (!checkLogin)
            {
                ViewBag.Message = "Bu işlemi gerçekleştirmek için giriş yapmanız gerekiyor.";
                return View("Msg");
            }

            Guid? userId = UserIdGetirr(nickname);
            if (!Guid.TryParse(HttpContext.Session.GetString("userId"), out var currentUserId) || userId != currentUserId)
            {
                ViewBag.Message = "Bu profili düzenleme yetkiniz yok.";
                return View("Msg");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                ViewBag.Message = "Kullanıcı bulunamadı.";
                return View("Msg");
            }

            return View(user);
        }


        [HttpPost]
        [Route("/duzenle/{id}")]
        public async Task<IActionResult> Duzenle(RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Geçersiz veri gönderildi.";
                return View("Msg");
            }

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

            //if (model.Image != null && model.Image.Length > 0)
            //{
            //    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);
            //    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            //    if (!Directory.Exists(uploadPath))
            //    {
            //        Directory.CreateDirectory(uploadPath);
            //    }

            //    var filePath = Path.Combine(uploadPath, fileName);

            //    using (var stream = new FileStream(filePath, FileMode.Create))
            //    {
            //        await model.Image.CopyToAsync(stream);
            //    }

            //    user.ImgUrl = $"/uploads/{fileName}";
            //}

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Profil başarıyla güncellendi.";
            return View("Msg");
        }
    }
}
