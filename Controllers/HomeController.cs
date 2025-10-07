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
using Microsoft.AspNetCore.SignalR;
using Flappr.Hubs;

namespace Flappr.Controllers
{
    public class HomeController : Controller
    {
        private readonly FlapprContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;

        // Dependency Injection (DI) ile IConfiguration, DbContext ve SignalR hub context'i alıyorum
        public HomeController(FlapprContext context, IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        // SMTP ayarları sadece bu sınıfta tanımlı. Diğer metotlarda tekrar etmemek için bu yardımcı (helper) metodu oluşturdum.
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
                Created = DateTime.UtcNow,
                Expiry = DateTime.UtcNow.AddMinutes(15),
                Used = false
            };

            _context.ResetPwTokens.Add(resetToken);
            _context.SaveChanges();

            return token;
        }

        [CustomAuthorize]
        public IActionResult Index()
        {
            var userIdString = HttpContext.Session.GetString("userId");
            Guid? userId = userIdString != null ? Guid.Parse(userIdString) : (Guid?)null;

            if (!string.IsNullOrEmpty(userIdString))
            {
                var unreadCount = _context.Notifications
                    .Count(n => n.UserId == userId.ToString() && !n.IsRead);

                ViewBag.UnreadCount = unreadCount;
            }
            else
            {
                ViewBag.UnreadCount = 0;
            }

            UserDto currentUser = null;
            if (userId != null)
            {
                currentUser = _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Nickname = u.Nickname,
                        Username = u.Username,
                        ImgUrl = u.ImgUrl
                    })
                    .FirstOrDefault();
            }
            ViewBag.CurrentUser = currentUser;

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
                    LikeCount = f.LikeCount,
                    IsLikedByCurrentUser = userId != null && _context.FlapLike.Any(l => l.FlapId == f.Id && l.UserId == userId)
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
                TempData["AuthError"] = "reCAPTCHA doğrulaması başarısız.";
                return View("Login");
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

            TempData["AuthError"] = "E-Posta veya şifre hatalı";
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
                TempData["AuthError"] = "Form eksik veya hatalı.";
                return View("Register");
            }

            var recaptchaValid = await VerifyRecaptchaRegister(model.RecaptchaToken);
            if (!recaptchaValid)
            {
                TempData["AuthError"] = "reCAPTCHA doğrulaması başarısız.";
                return View("Register", model);
            }

            if (model.Password != model.Pwconfirmend)
                {
                TempData["AuthError"] = "Şifreler Uyuşmuyor.";
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
                    (true, true) => "Kullanıcı adı ve e-posta zaten kullanılıyor!",
                    (true, false) => "Bu kullanıcı adı mevcut!",
                    (false, true) => "Bu e-posta adresi zaten kullanılıyor!",
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

            string subject = "Flappr’a Hoş Geldiniz!";

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

                return result.success == true && result.score >= 0.7 && result.action == "register";
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
                TempData["AuthError"] = "Böyle bir hesap bulunamadı! Lütfen kayıt olun veya farklı bir hesapla giriş yapın.";
                return View("Login");
            }

            HttpContext.Session.SetString("userId", user.Id.ToString());
            HttpContext.Session.SetString("Mail", user.Mail);
            HttpContext.Session.SetString("Nickname", user.Nickname);

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

            var userIdStr = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(dto.Detail))
            {
                TempData["FlapMessage"] = "Flap içeriği boş olamaz!";
                return RedirectToAction("Index");
            }

            if (dto.Detail.Length > 200)
            {
                TempData["FlapMessage"] = "Flap en fazla 200 karakter olabilir!";
                return RedirectToAction("Index");
            }

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

            var userIdString = HttpContext.Session.GetString("userId");
            Guid? userId = userIdString != null ? Guid.Parse(userIdString) : (Guid?)null;

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
                UserImgUrl = flapEntity.User.ImgUrl,
                Flap = flapEntity,
                LikeCount = flapEntity.LikeCount,
                IsLikedByCurrentUser = userId != null && _context.FlapLike.Any(l => l.FlapId == flapEntity.Id && l.UserId == userId),
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
                Biography = user.Biography,
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
                    CommentsCount = _context.Comments.Count(c => c.FlapId == f.Id),
                    LikeCount = _context.FlapLike.Count(l => l.FlapId == f.Id),
                    IsLikedByCurrentUser = currentUserId != null &&
                               _context.FlapLike.Any(l => l.FlapId == f.Id && l.UserId == currentUserId)
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

            if (string.IsNullOrWhiteSpace(Summary))
            {
                TempData["CommentMessage"] = "Yorum boş olamaz!";
                return RedirectToAction("Flap", new { Id = FlapId });
            }

            if (Summary.Length > 200)
            {
                TempData["CommentMessage"] = "Yorum 200 karakteri geçemez!";
                return RedirectToAction("Flap", new { Id = FlapId });
            }
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

            if (flap != null && flap.UserId != comment.UserId)
            {
                var flapOwner = flap.User;
                if (!string.IsNullOrWhiteSpace(flapOwner.Mail))
                {
                    using var reader = new StreamReader("wwwroot/mailTemp/mailtemp.html");
                    var template = await reader.ReadToEndAsync();
                    var mailbody = template
                        .Replace("{{Username}}", flapOwner.Username)
                        .Replace("{{FlapDetail}}", flap.Detail);

                    string subject = "Flap’ınıza yeni bir yorum yapıldı!";
                    await SendEmailAsync(flapOwner.Mail, subject, mailbody);
                }
                var notification = new Notification
                {
                    UserId = flapOwner.Id.ToString(),
                    SenderId = user.Id.ToString(),
                    Type = "Yeni yorum 🎉",
                    Message = $"{user.Nickname}, flap’ınıza yorum yaptı.",
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(flapOwner.Id.ToString())
                    .SendAsync("ReceiveNotification", notification.Message);
            }

            return RedirectToAction("Flap", new { Id = FlapId });
        }

        [Route("/YorumSil/{Id}")]
        public IActionResult DeleteYorum(Guid Id, Guid FlapId)
        {
            var comment = _context.Comments.Find(Id);
            if (comment == null)
                return NotFound();

            var flap = _context.Flaps.Find(FlapId);
            if (flap == null)
                return NotFound();

            _context.Comments.Remove(comment);
            _context.SaveChanges();

            return RedirectToAction("Flap", new { Id = FlapId });
        }


        [Route("/Flapsil/{Id}")]
        public IActionResult FlapSil(Guid Id, string nickname)
        {
            var flap = _context.Flaps
                               .Include(f => f.Likes)
                               .FirstOrDefault(f => f.Id == Id);

            if (flap != null)
            {
                _context.FlapLike.RemoveRange(flap.Likes);

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
                TempData["PwResetErrorMessage"] = "Bu e-posta adresiyle kayıtlı bir kullanıcı bulunamadı !";
                return View();
            }

            return RedirectToAction("PwResetLink", new { userId = user.Id });
        }

        public async Task<IActionResult> PwResetLink(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                TempData["PwResetErrorMessage"] = "Bu e-posta adresiyle kayıtlı bir kullanıcı bulunamadı !";
                return Redirect("/sifre-unuttum");
            }

            var token = TokenUret(userId);

            var resetLink = Url.Action("ResetPassword", "Admin", new { token }, Request.Scheme);

            using var reader = new StreamReader("wwwroot/mailTemp/pwreset.html");
            var template = await reader.ReadToEndAsync();
            var resetUrl = $"https://flappr.polatturkk.com.tr/pwresetform?token={token}";
            var mailBody = template.Replace("{{ResetLink}}", resetUrl);
            string subject = "Şifre Sıfırlama Talebi";

            await SendEmailAsync(user.Mail, subject, mailBody);

            TempData["PwResetSuccessMessage"] = "Şifre sıfırlama mail olarak iletilmiştir.";
            return Redirect("/sifre-unuttum");
        }

        [HttpGet]
        [Route("/PwResetForm")]
        public IActionResult PwResetForm(string token)
        {
            var model = new PwResetRequest { Token = token };
            return View(model);
        }

        [HttpPost]
        [Route("/PwResetForm")]
        public async Task<IActionResult> PwResetForm(PwResetRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["PwResetErrorMessage"] = "Şifreler eşleşmiyor !";
                return Redirect("/PwResetForm");
            }

            var userToken = await _context.ResetPwTokens
                .FirstOrDefaultAsync(t => t.Token == model.Token && !t.Used);

            if (userToken == null || userToken.Expiry < DateTime.UtcNow)
            {
                TempData["PwResetErrorMessage"] = "Token geçersiz veya süresi dolmuş !";
                return Redirect("/PwResetForm");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userToken.UserId);
            if (user == null)
            {
                TempData["PwResetErrorMessage"] = "Kullanıcı bulunamadı !";
                return Redirect("/PwResetForm");
            }

            user.Password = Helper.Hash(model.NewPassword);

            userToken.Used = true;

            await _context.SaveChangesAsync();

            TempData["PwResetSuccessMessage"] = "Şifreniz başarıyla değiştirildi.";
            return Redirect("/PwResetForm");
        }

        [CustomAuthorize][HttpGet]
        public IActionResult Search(){return View(new SearchRequest());}

        [HttpPost]
        public async Task<IActionResult> Search(SearchRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                TempData["PwResetErrorMessage"] = "Lütfen bir arama terimi girin.";
                return View(model);
            }

            var searchTerm = model.SearchTerm.ToLower();

            var results = await _context.Users
                .Where(u => u.Username.ToLower().Contains(searchTerm)
                         || u.Nickname.ToLower().Contains(searchTerm))
                .Select(u => new SearchResponse
                {
                    Id = u.Id,
                    UserUsername = u.Username,
                    UserNickname = u.Nickname
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
