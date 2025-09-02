using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Flappr.Models;
using System.Net.Mail;
using System.Net;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Flappr.Data;
using Microsoft.EntityFrameworkCore;
using Flappr.Dto;

namespace Flappr.Controllers
{
    //butun proje Dapperdan EF Core'a gecicek /  geciyor..
    public class HomeController : Controller
    {
            private readonly FlapprContext _context;
            private readonly IConfiguration _configuration;

            //Dependency Injection (DI) ile hem IConfiguration hem DbContext alýyorum
            public HomeController(FlapprContext context, IConfiguration configuration)
            {
                _context = context;
                _configuration = configuration;
            }

        // SMTP ayarlarý sadece burada var diðer metodlarda tekrar yazmamak icin boyle bir helper metodu yaptim
        private async Task SendEmailAsync(string toEmail, string subject, string body, string displayName = "Flappr Ekibi")
        {
            var host = _configuration["Smtp:Host"];
            var port = int.Parse(_configuration["Smtp:Port"]);
            var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"]);
            var username = _configuration["Smtp:UserName"];
            var password = _configuration["Smtp:Password"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(username, displayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(toEmail));

            await client.SendMailAsync(mailMessage);
        }

        public bool CheckLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("nickname")))
            {
                return false;
            }

            return true;
        }
        public string TokenUret(int userId)
        {
            var token = Guid.NewGuid().ToString();

            var resetToken = new ResetPwToken
            {
                UserId = userId,
                Token = token,
                Created = DateTime.Now,
                Used = false
            };

            _context.ResetPwTokens.Add(resetToken);
            _context.SaveChanges();

            return token;
        }

        public Register GetMail(string email)
        {
            return _context.Users
               .FirstOrDefault(u => u.Mail == email);
        }
        public int? KullaniciGetir(string nickname)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Nickname == nickname);

            return user?.Id;
        }

        public bool FlapVarMi(int id)
        {
            return _context.Flaps.Any(f => f.Id == id);
        }

        public IActionResult Index()
        {
            ViewData["Nickname"] = HttpContext.Session.GetString("nickname");

            var flaps = _context.Flaps
                .Where(f => f.Visibility)
                .Select(f => new Flap
                {
                    Id = f.Id,
                    Detail = f.Detail,
                    Username = f.Username,
                    CreatedDate = f.CreatedDate,
                    Nickname = f.Nickname,
                    ImgUrl = f.ImgUrl,
                    YorumSayisi = _context.Comments.Count(c => c.FlapId == f.Id)
                })
                .OrderByDescending(f => f.CreatedDate)
                .ToList();

            return View(flaps);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new Register());
        }


        [HttpPost]
        [Route("/Login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["AuthError"] = "Form eksik.";
                return RedirectToAction("Login");
            }

            // reCAPTCHA doðrulamasý
            var recaptchaValid = await VerifyRecaptchaLogin(model.RecaptchaToken);
            if (!recaptchaValid)
            {
                TempData["AuthError"] = "reCAPTCHA doðrulamasý baþarýsýz.";
                return RedirectToAction("Login");
            }

            // Þifre hash'lemesi
            var hashedPassword = Helper.Hash(model.Password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Nickname == model.Nickname && u.Password == hashedPassword);

            if (user != null)
            {
                HttpContext.Session.SetInt32("userId", user.Id);
                HttpContext.Session.SetString("Nickname", user.Nickname);
                ViewData["Nickname"] = HttpContext.Session.GetString("Nickname");

                ViewBag.Message = "Login Baþarýlý";
                return View("Message");
            }

            TempData["AuthError"] = "Kullanýcý adý veya þifre hatalý";
            return View("Login");
        }


        private async Task<bool> VerifyRecaptchaLogin(string token)
        {
            string secretKey = _configuration["Recaptcha:SecretKey"];

            using var client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "secret", secretKey },
                { "response", token }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            var responseString = await response.Content.ReadAsStringAsync();

            dynamic result = JsonConvert.DeserializeObject(responseString);

            return result.success == true && result.score >= 0.6 && result.action == "login";
        }


        [HttpPost]
        [Route("/KayitOl")]
        public async Task<IActionResult> KayitOl(RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["AuthError"] = "Form eksik veya hatalý.";
                return View("Register");
            }

            if (string.IsNullOrWhiteSpace(model.RecaptchaToken))
            {
                TempData["AuthError"] = "reCAPTCHA doðrulama bilgisi eksik.";
                return View("Register", model);
            }

            // reCAPTCHA doðrulamasý
            var recaptchaValid = await VerifyRecaptchaRegister(model.RecaptchaToken);
            if (!recaptchaValid)
            {
                TempData["AuthError"] = "reCAPTCHA doðrulamasý baþarýsýz.";
                return View("Register", model);
            }

            if (model.Password != model.Pwconfirmend)
            {
                TempData["AuthError"] = "Þifreler Uyuþmuyor.";
                return View("Register", model);
            }

            // Kullanýcý var mý kontrolü EF Core ile
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Nickname == model.Nickname);

            if (existingUser != null)
            {
                TempData["AuthError"] = "Bu kullanýcý adý mevcut!";
                return View("Register", model);
            }

            // Yeni kullanýcý oluþturma
            var user = new Register
            {
                Username = model.Username,
                Nickname = model.Nickname,
                Mail = model.Mail,
                RoleId = 1,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                ImgUrl = "/uploads/images.jpg",
                Password = Helper.Hash(model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Mail gönderimi
            string subject = "Flappr Hoþgeldin mesajý";
            string body = $"Merhaba {model.Username}. Flappr Kaydýnýz baþarýlý bir þekilde oluþturulmuþtur.";

            await SendEmailAsync(model.Mail, subject, body);

            ViewBag.Message = "Kayýt Baþarýlý";
            return View("Message");
        }


        private async Task<bool> VerifyRecaptchaRegister(string token)
        {
            string secretKey = _configuration["Recaptcha:SecretKey"];

            try
            {
                using var client = new HttpClient();

                var values = new Dictionary<string, string>
                {
                   { "secret", secretKey },
                   { "response", token }
                };

                using var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

                if (!response.IsSuccessStatusCode)
                    return false;

                var responseString = await response.Content.ReadAsStringAsync();

                dynamic result = JsonConvert.DeserializeObject(responseString);

                return result.success == true && result.score >= 0.6 && result.action == "register";
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;


            HttpContext.Session.SetString("nickname", name ?? email);

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult GithubLogin()
        {
            var redirectUrl = Url.Action("GithubResponse", "Home");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "GitHub");
        }

        [HttpGet]
        public async Task<IActionResult> GithubResponse()
        {
            var result = await HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Identities
                    .FirstOrDefault()?.Claims
                    .ToDictionary(c => c.Type, c => c.Value);

            if (claims.ContainsKey(ClaimTypes.Name))
            {
                HttpContext.Session.SetString("nickname", claims[ClaimTypes.Name]);
                ViewData["Nickname"] = claims[ClaimTypes.Name];
            }
            else if (claims.ContainsKey("urn:github:login")) // fallback
            {
                HttpContext.Session.SetString("nickname", claims["urn:github:login"]);
                ViewData["Nickname"] = claims["urn:github:login"];
            }

            return RedirectToAction("Index");
        }


        public IActionResult Cikis()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        //devam edilecek...

        //[Route("/profil/{nickname}")]
        //public IActionResult Profile(string nickname)
        //{
        //    ViewData["nickname"] = HttpContext.Session.GetString("nickname");
        //    int? userId = KullaniciGetir(nickname);
        //    if (userId == null)
        //    {
        //        ViewBag.Message = "Böyle bir kullanýcý yok!";
        //        return View("Message");
        //    }

        //    var profil = new Profile();
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        if (userId == HttpContext.Session.GetInt32("userId"))
        //        {
        //            ViewBag.profile = true;
        //            var sql =
        //                "SELECT Detail, users.Username , CreatedDate, Visibility  FROM Flaps LEFT JOIN users on Flaps.UserId = users.Id WHERE UserId = @userId ORDER BY CreatedDate DESC";
        //            var Flaps = connection.Query<Flap>(sql, new { UserId = userId }).ToList();
        //            profil.Flaps = Flaps;
        //        }
        //        else
        //        {
        //            ViewBag.profile = false;
        //            var sql =
        //                "SELECT Detail, users.Username as Username, CreatedDate, Visibility  FROM Flaps LEFT JOIN users on Flaps.UserId = users.Id WHERE UserId = @userId AND Visibility = 1 ORDER BY CreatedDate DESC";
        //            var Flaps = connection.Query<Flap>(sql, new { UserId = userId }).ToList();
        //            profil.Flaps = Flaps;
        //        }
        //    }

        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        var sql = "SELECT * FROM Users WHERE Id = @userId";
        //        var profile = connection.QueryFirstOrDefault<Register>(sql, new { UserId = userId });
        //        profil.User = profile;
        //    }


        //    return View(profil);
        //}

        //[HttpPost]
        //[Route("/AddFlap")]
        //public IActionResult AddFlap(Flap model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.Message = "Eksik veya hatalý iþlem yaptýn.";
        //        return View("Message");
        //    }

        //    model.CreatedDate = DateTime.Now;
        //    model.UserId = (int)HttpContext.Session.GetInt32("userId");

        //    using var connection = new SqlConnection(connectionString);
        //    var sql =
        //        "INSERT INTO Flaps (Detail, UserId, CreatedDate, Visibility) VALUES (@Detail, @UserId, @CreatedDate, @Visibility)";

        //    var data = new
        //    {
        //        model.Detail,
        //        model.UserId,
        //        model.CreatedDate,
        //        model.Visibility
        //    };

        //    var rowsAffected = connection.Execute(sql, data);

        //    return RedirectToAction("Index");
        //}

        //[Route("/Flap/{Id}")]
        //public IActionResult Flap(int Id)
        //{
        //    if (!FlapVarMi(Id))
        //    {
        //        ViewBag.Message = "böyle bir Flap yok";
        //        return View("Message");
        //    }

        //    ViewData["Nickname"] = HttpContext.Session.GetString("nickname");


        //    ViewBag.AddYorum = true;
        //    if (!CheckLogin())
        //    {
        //        ViewBag.AddYorum = false;
        //    }

        //    var detailFlap = new DetailFlap();

        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        var sql =
        //            "SELECT Flaps.Id ,UserId ,Detail, users.Username as Username, CreatedDate, users.Nickname as Nickname, users.ImgUrl as ImgUrl FROM Flaps LEFT JOIN users on Flaps.UserId = users.Id WHERE Flaps.Id = @Id";
        //        var Flap = connection.QueryFirstOrDefault<Flap>(sql, new { Id = Id });
        //        detailFlap.Flap = Flap;
        //    }

        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        var sql =
        //            "SELECT  comments.Id ,UserId ,Summary, users.Username, users.Nickname, users.ImgUrl, CreatedTime FROM comments LEFT JOIN users on users.Id = comments.UserId WHERE FlapId = @Id ORDER BY CreatedTime DESC";
        //        var comments = connection.Query<Comment>(sql, new { Id }).ToList();
        //        detailFlap.Comments = comments;
        //    }

        //    if (detailFlap.Flap.UserId == HttpContext.Session.GetInt32("userId"))
        //    {
        //        ViewBag.yetki = "full";
        //    }

        //    ViewBag.id = HttpContext.Session.GetInt32("userId");

        //    return View(detailFlap);
        //}

        //[HttpPost]
        //[Route("/addyorum")]
        //public async Task<IActionResult> AddYorum(Comment model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return RedirectToAction("Index");
        //    }

        //    model.CreatedTime = DateTime.Now;
        //    model.UserId = (int)HttpContext.Session.GetInt32("userId");

        //    using var connection = new SqlConnection(connectionString);
        //    var sql =
        //        "INSERT INTO comments (Summary, CreatedTime, UserId, FlapId) VALUES (@Summary, @CreatedTime, @UserId, @FlapId)";

        //    try
        //    {
        //        var affectedRows = connection.Execute(sql, model);

        //        using var cnt = new SqlConnection(connectionString);
        //        var cntsql =
        //            "SELECT users.Mail, Flaps.Detail, users.Username " +
        //            "FROM comments " +
        //            "LEFT JOIN Flaps on comments.FlapId = Flaps.Id " +
        //            "LEFT JOIN users on Flaps.UserId = users.Id " +
        //            "WHERE comments.FlapId = @FlapId";

        //        var FlapInfo = cnt.QueryFirstOrDefault<FlapInfo>(cntsql, new { FlapId = model.FlapId });

        //        using var reader = new StreamReader("wwwroot/mailTemp/mailtemp.html");
        //        var template = await reader.ReadToEndAsync();
        //        var mailbody = template
        //            .Replace("{{Username}}", FlapInfo.Username)
        //            .Replace("{{FlapDetail}}", FlapInfo.Detail);

        //        string subject = "Flappýnýza bildirim var";
        //        await SendEmailAsync(FlapInfo.Mail, subject, mailbody);

        //        return RedirectToAction("Flap", new { Id = model.FlapId });
        //    }
        //    catch (Exception ex)
        //    {
        //        return RedirectToAction("Index");
        //    }
        //}


        //[Route("/YorumSil/{Id}")]
        //public IActionResult DeleteYorum(int Id, int FlapId)
        //{
        //    using var connection = new SqlConnection(connectionString);


        //    var sql = "DELETE FROM comments WHERE Id = @Id";
        //    var rowsAffected = connection.Execute(sql, new { Id = Id });

        //    return RedirectToAction("Flap", new { Id = FlapId });
        //}

        //[Route("/Flapsil/{Id}")]
        //public IActionResult FlapSil(int Id, string nickname)
        //{

        //    using var connection = new SqlConnection(connectionString);

        //    var sql = "DELETE FROM Flaps WHERE Id = @Id";
        //    var rowsAffected = connection.Execute(sql, new { Id = Id });

        //    return RedirectToAction("Profile", new { nickname });
        //}

        //[HttpGet]
        //[Route("/sifre-unuttum")]
        //public IActionResult SifreUnuttum()
        //{
        //    return View();
        //}

        //[HttpPost]
        //[Route("/SifreUnuttum")]
        //public IActionResult SifreUnuttum(string email)
        //{
        //    using var connection = new SqlConnection(connectionString);

        //    var user = connection.QueryFirstOrDefault<Register>(
        //        "SELECT * FROM users WHERE Mail = @Mail", new { Mail = email });

        //    if (user == null)
        //    {
        //        ViewBag.Message = "Bu e-posta adresiyle kayýtlý bir kullanýcý bulunamadý.";
        //        return View("Message");
        //    }

        //    return RedirectToAction("PwResetLink", new { userId = user.Id });
        //}

        //public async Task<IActionResult> PwResetLink(int userId)
        //{
        //    using var connection = new SqlConnection(connectionString);
        //    string userEmail = connection.QueryFirstOrDefault<string>("SELECT Mail FROM users WHERE Id = @UserId", new { UserId = userId });

        //    var token = TokenUret(userId);

        //    var resetLink = Url.Action("ResetPassword", "Admin", new { token }, Request.Scheme);

        //    using var reader = new StreamReader("wwwroot/mailTemp/pwreset.html");
        //    var template = await reader.ReadToEndAsync();
        //    var mailBody = template.Replace("{{Resetlink}}", resetLink);

        //    string subject = "Þifre Sýfýrlama Talebi";

        //    // Mail gönderimi async helper üzerinden
        //    await SendEmailAsync(userEmail, subject, mailBody);

        //    ViewBag.Message = "Þifre sýfýrlama mail olarak iletilmiþtir.";
        //    return View("Message");
        //}

        //[HttpGet]
        //public IActionResult Search()
        //{
        //    return View(new AramaModel());
        //}

        //[HttpPost]
        //public IActionResult Search(AramaModel model)
        //{
        //    if (string.IsNullOrWhiteSpace(model.SearchTerm))
        //    {
        //        ModelState.AddModelError("", "Lütfen bir arama terimi girin.");
        //        return View(model);
        //    }

        //    using var connection = new SqlConnection(connectionString);
        //    var sql = "SELECT Id, Username, Nickname FROM users WHERE Username LIKE @SearchTerm OR Nickname LIKE @SearchTerm";

        //    var searchTerm = "%" + model.SearchTerm + "%";
        //    model.Sonuc = connection.Query<Arama>(sql, new { SearchTerm = searchTerm }).ToList();

        //    return View(model);
        //}
    }
}
