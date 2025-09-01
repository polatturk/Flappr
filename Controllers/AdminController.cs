using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Flappr.Models;
using Microsoft.AspNetCore.Authentication;

namespace Flappr.Controllers
{
    public class AdminController : Controller
    {
        string connectionString = "";
        public bool CheckLoginn()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("nickname")))
            {
                return false;
            }

            return true;
        }

        public int? UserIdGetirr(string nickname)
        {
            using var connection = new SqlConnection(connectionString);
            var sql = "SELECT Id FROM users WHERE Nickname = @nickname";
            var userId = connection.QueryFirstOrDefault<int?>(sql, new { Nickname = nickname });
            return userId;
        }

        [Route("/duzenle/{nickname}")]
        public IActionResult Duzenle(string nickname)
        {
            ViewData["Nickname"] = HttpContext.Session.GetString("nickname");

            var checkLogin = CheckLoginn();
            if (!checkLogin)
            {
                ViewBag.Message = "Login Ol.";
                return View("Msg");
            }

            int? userId = UserIdGetirr(nickname);
            if (userId != HttpContext.Session.GetInt32("userId"))
            {
                ViewBag.Message = "Ne yapıyorsun?";
                return View("Msg");
            }

            using var connection = new SqlConnection(connectionString);
            var sql = "SELECT * FROM users WHERE Id = @userId";
            var users = connection.QueryFirstOrDefault<Register>(sql, new { UserId = userId });

            return View(users);
        }

        [HttpPost]
        [Route("/duzenle/{id}")]
        public IActionResult Duzenle(Register model)
        {
            using var connection = new SqlConnection(connectionString);
            var sql =
                "UPDATE users SET Username = @username, Password = @Password, ImgUrl = @ImgUrl WHERE Id = @Id";
            var Password = Guid.NewGuid().ToString() + Path.GetExtension(model.Password);

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            using var stream = new FileStream(path, FileMode.Create);
            model.Image.CopyTo(stream);
            model.ImgUrl = $"/uploads/{Password}";

            var data = new
            {
                model.Username,
                model.Password,
                model.ImgUrl,
                model.Id
            };

            var rowAffected = connection.Execute(sql, data);

            ViewBag.Message = "Profil Güncellendi.";
            return View("Msg");

        }
    }
}
