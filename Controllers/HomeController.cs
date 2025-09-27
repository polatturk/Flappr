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
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Flappr.Filters;

namespace Flappr.Controllers
{
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
            var userMail = HttpContext.Session.GetString("Mail");
            return !string.IsNullOrEmpty(userMail);
        }
        public string TokenUret(Guid userId)
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

        public User GetMail(string email){return _context.Users.FirstOrDefault(u => u.Mail == email);}
        public Guid? KullaniciGetir(string nickname){var user = _context.Users.FirstOrDefault(u => u.Nickname == nickname);return user.Id;}

        public bool FlapVarMi(Guid id)
        {
            return _context.Flaps.Any(f => f.Id == id);
        }

        [CustomAuthorize]
        public IActionResult Index()
        {
            ViewData["Nickname"] = HttpContext.Session.GetString("nickname");

            var flaps = _context.Flaps
                .Where(f => f.Visibility)
                .Include(f => f.User)
                .Select(f => new FlapRequest
                {
                    Id = f.Id,
                    Detail = f.Detail,
                    CreatedDate = f.CreatedDate,
                    UserNickname = f.User.Nickname,
                    UserUsername = f.User.Username,
                    UserImgUrl = f.User.ImgUrl,
                    CommentsCount = _context.Comments.Count(c => c.FlapId == f.Id),
                    LikeCount = f.LikeCount
                })
                .OrderByDescending(f => f.CreatedDate)
                .ToList();

            return View(flaps);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(){return View();}

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register(){return View(new RegisterRequest());}

        [HttpPost]
        [Route("/Login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["AuthError"] = "Form eksik.";
                return View("Login");
            }

            var recaptchaValid = await VerifyRecaptchaLogin(model.RecaptchaToken);
            if (!recaptchaValid)
            {
                TempData["AuthError"] = "reCAPTCHA doðrulamasý baþarýsýz.";
                return RedirectToAction("Login");
            }

            var hashedPassword = Helper.Hash(model.Password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Mail == model.Mail && u.Password == hashedPassword);

            if (user != null)
            {
                HttpContext.Session.SetString("userId", user.Id.ToString());
                HttpContext.Session.SetString("Mail", user.Mail);
                HttpContext.Session.SetString("Nickname", user.Nickname);
                HttpContext.Session.SetString("Username", user.Username);

                return RedirectToAction("Index", "Home");
            }

            TempData["AuthError"] = "E-Posta veya þifre hatalý";
            return RedirectToAction("Login");
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

            var userExists = await _context.Users
              .Where(u => u.Nickname == model.Nickname || u.Mail == model.Mail)
              .Select(u => new { u.Nickname, u.Mail })
              .ToListAsync();

            if (userExists.Any())
            {
                bool nicknameTaken = userExists.Any(u => u.Nickname == model.Nickname);
                bool mailTaken = userExists.Any(u => u.Mail == model.Mail);

                TempData["AuthError"] = (nicknameTaken, mailTaken) switch
                {
                    (true, true) => "Kullanýcý adý ve e-posta zaten kullanýlýyor!",
                    (true, false) => "Bu kullanýcý adý mevcut!",
                    (false, true) => "Bu e-posta adresi zaten kullanýlýyor!",
                    _ => null
                };

                return View("Register", model);
            }

            var user = new User
            {
                Username = model.Username,
                Nickname = model.Nickname,
                Mail = model.Mail,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                ImgUrl = "/uploads/images.jpg",
                Password = Helper.Hash(model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            using var reader = new StreamReader("wwwroot/mailTemp/register.html");
            var template = await reader.ReadToEndAsync();

            var mailBody = template
                .Replace("{{Username}}", model.Username)
                .Replace("{{Nickname}}", model.Nickname)
                .Replace("{{LoginLink}}", "https://flappr.polatturkk.com.tr/home/login");

            string subject = "Flappr’a Hoþ Geldiniz!";

            await SendEmailAsync(model.Mail, subject, mailBody);

            return View("Login");
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

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var emailClaim = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

            var user = await _context.Users
           .FirstOrDefaultAsync(u => u.Mail.ToLower() == emailClaim.ToLower());

            if (user == null)
            {
                TempData["AuthError"] = "Böyle bir hesap bulunamadý! Lütfen kayýt olun veya farklý bir hesapla giriþ yapýn.";
                return View("Login");
            }

            HttpContext.Session.SetString("userId", user.Id.ToString());
            HttpContext.Session.SetString("Mail", user.Mail);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Cikis()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/Home/Login");
        }

        [HttpPost]
        [Route("/AddFlap")]
        public IActionResult AddFlap(AddFlapDto dto)
        {
            if (!ModelState.IsValid) return View("Index");

            var userIdStr = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            var flap = new Flap
            {
                Detail = dto.Detail,
                Visibility = dto.Visibility,
                UserId = Guid.Parse(userIdStr),
                CreatedDate = DateTime.Now
            };

            _context.Flaps.Add(flap);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        [CustomAuthorize]
        [Route("/Flap/{Id}")]
        public IActionResult Flap(Guid Id)
        {
            var flapEntity = _context.Flaps
                .Include(f => f.User)
                .FirstOrDefault(f => f.Id == Id);

            if (flapEntity == null)
            {
                ViewBag.Message = "Böyle bir Flap yok";
                return View("Message");
            }

            ViewData["Nickname"] = HttpContext.Session.GetString("Nickname");
            ViewBag.AddYorum = CheckLogin();

            var detailFlap = new FlapRequest
            {
                Id = flapEntity.Id,
                Detail = flapEntity.Detail,
                CreatedDate = flapEntity.CreatedDate,
                UserUsername = flapEntity.User.Username,
                UserNickname = flapEntity.User.Nickname,
                Flap = flapEntity,
                Comments = _context.Comments
                    .Where(c => c.FlapId == flapEntity.Id)
                    .OrderByDescending(c => c.CreatedTime)
                    .Select(c => new Comment
                    {
                        Id = c.Id,
                        Summary = c.Summary,
                        CreatedTime = c.CreatedTime,
                        UserId = c.UserId,
                        Username = c.User.Username,
                        Nickname = c.User.Nickname,
                        ImgUrl = c.User.ImgUrl
                    })
                    .ToList()
            };

            if (flapEntity.UserId == Guid.Parse(HttpContext.Session.GetString("userId")!))
            {
                ViewBag.yetki = "full";
            }

            var userIdString = HttpContext.Session.GetString("userId");
            Guid? userId = userIdString != null ? Guid.Parse(userIdString) : (Guid?)null;
            ViewBag.id = userId;
            return View(detailFlap);
        }

        [CustomAuthorize]
        [Route("/Profile/{UserNickname?}")]
        public async Task<IActionResult> Profile(string? UserNickname)
        {
            var currentUserIdString = HttpContext.Session.GetString("userId");
            Guid? currentUserId = currentUserIdString != null ? Guid.Parse(currentUserIdString) : (Guid?)null;

            if (string.IsNullOrEmpty(UserNickname))
            {
                var userIdString = HttpContext.Session.GetString("userId");
                Guid? userId = userIdString != null ? Guid.Parse(userIdString) : (Guid?)null;
                if (userId == null) return RedirectToAction("Login");

                UserNickname = (await _context.Users.FindAsync(userId))?.Username;
            }

            var user = await _context.Users
           .FirstOrDefaultAsync(u => u.Username == UserNickname || u.Nickname == UserNickname);

            var currentUserIsOwner = user.Id == currentUserId;
            ViewBag.IsOwner = currentUserIsOwner;

            bool isFollowing = false;
            if (currentUserId != null && !currentUserIsOwner)
            {
                isFollowing = await _context.Follows
                    .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == user.Id);
            }
            ViewBag.IsFollowing = isFollowing;

            var userDto = new UserDto
            {
                Id = user.Id,
                Nickname = user.Nickname,
                Username = user.Username,
                Mail = user.Mail,
                ImgUrl = user.ImgUrl
            };

            bool isOwner = Guid.TryParse(HttpContext.Session.GetString("userId"), out var id) && user.Id == id;
            ViewBag.profile = isOwner;

            var flapsQuery = _context.Flaps
                .Where(f => f.UserId == user.Id);

            if (!isOwner)
            {
                flapsQuery = flapsQuery.Where(f => f.Visibility);
            }

            var flapDtos = await flapsQuery
                .Include(f => f.User)
                .OrderByDescending(f => f.CreatedDate)
                .Select(f => new FlapDto
                {
                    Id = f.Id,
                    Detail = f.Detail,
                    Username = f.User.Username,
                    Nickname = f.User.Nickname,
                    ImgUrl = f.User.ImgUrl,
                    Visibility = f.Visibility,
                    CreatedDate = f.CreatedDate,
                    CommentsCount = f.CommentCount
                })
                .ToListAsync();

            var followersCount = await _context.Follows
                .CountAsync(f => f.FollowingId == user.Id);

            var followingCount = await _context.Follows
                .CountAsync(f => f.FollowerId == user.Id);

            var profilDto = new ProfileRequest
            {
                User = userDto,
                Flaps = flapDtos,
                FollowersCount = followersCount,
                FollowingCount = followingCount
            };

            return View(profilDto);
        }

        [HttpPost]
        [Route("/addyorum")]
        public async Task<IActionResult> AddYorum(Guid FlapId, string Summary)
        {
            var userIdString = HttpContext.Session.GetString("userId");
            Guid? userId = userIdString != null ? Guid.Parse(userIdString) : (Guid?)null;
            if (userId == null) return RedirectToAction("Login"); 

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null || string.IsNullOrWhiteSpace(Summary))
                return RedirectToAction("Flap", new { Id = FlapId });

            var comment = new Comment
            {
                FlapId = FlapId,
                Summary = Summary,
                UserId = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                CreatedTime = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var flap = await _context.Flaps.Include(f => f.User)
                        .FirstOrDefaultAsync(f => f.Id == FlapId);

            if (flap != null)
            {
                if (comment.UserId != flap.UserId)
                {
                    using var reader = new StreamReader("wwwroot/mailTemp/mailtemp.html");
                    var template = await reader.ReadToEndAsync();
                    var mailbody = template
                        .Replace("{{Username}}", flap.User.Username)
                        .Replace("{{FlapDetail}}", flap.Detail);

                    string subject = "Flap’ýnýza yeni bir yorum yapýldý!";
                    await SendEmailAsync(flap.User.Mail, subject, mailbody);
                }
            }

            return RedirectToAction("Flap", new { Id = FlapId });
        }

        [Route("/YorumSil/{Id}")]
        public IActionResult DeleteYorum(Guid Id, Guid FlapId)
        {
            var comment = _context.Comments.Find(Id);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                _context.SaveChanges();
            }

            return RedirectToAction("Flap", new { Id = FlapId });
        }

        [Route("/Flapsil/{Id}")]
        public IActionResult FlapSil(Guid Id, string nickname)
        {
            var flap = _context.Flaps.Find(Id);
            if (flap != null)
            {
                _context.Flaps.Remove(flap);
                _context.SaveChanges();
            }

            return RedirectToAction("Profile", new { nickname });
        }

        [AllowAnonymous][HttpGet]
        [Route("/sifre-unuttum")]
        public IActionResult SifreUnuttum(){return View();}

        [HttpPost]
        [Route("/SifreUnuttum")]
        public async Task<IActionResult> SifreUnuttum(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Mail == email);

            if (user == null)
            {
                ViewBag.Message = "Bu e-posta adresiyle kayýtlý bir kullanýcý bulunamadý.";
                return View("Message");
            }

            return RedirectToAction("PwResetLink", new { userId = user.Id });
        }

        public async Task<IActionResult> PwResetLink(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                ViewBag.Message = "Kullanýcý bulunamadý.";
                return View("Message");
            }

            var token = TokenUret(userId);

            var resetLink = Url.Action("ResetPassword", "Admin", new { token }, Request.Scheme);

            using var reader = new StreamReader("wwwroot/mailTemp/pwreset.html");
            var template = await reader.ReadToEndAsync();
            var mailBody = template.Replace("{{Resetlink}}", resetLink);

            string subject = "Þifre Sýfýrlama Talebi";

            await SendEmailAsync(user.Mail, subject, mailBody);

            ViewBag.Message = "Þifre sýfýrlama mail olarak iletilmiþtir.";
            return View("Message");
        }

        [CustomAuthorize][HttpGet]
        public IActionResult Search(){return View(new SearchRequest());}

        [HttpPost]
        public async Task<IActionResult> Search(SearchRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                ModelState.AddModelError("", "Lütfen bir arama terimi girin.");
                return View(model);
            }

            var searchTerm = model.SearchTerm.ToLower();

            var results = await _context.Users
                .Where(u => u.Username.ToLower().Contains(searchTerm)
                         || u.Nickname.ToLower().Contains(searchTerm))
                .Select(u => new SearchResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Nickname = u.Nickname
                })
                .ToListAsync();

            var response = new SearchRequest
            {
                Sonuc = results
            };

            return View(response);
        }

        [AllowAnonymous]
        public IActionResult Info(){return View();}
        [AllowAnonymous]
        public IActionResult Privacy() { return View(); }
        [AllowAnonymous]
        public IActionResult TermsofService() { return View(); }

    }
}
