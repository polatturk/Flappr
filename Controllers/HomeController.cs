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

        public User GetMail(string email){return _context.Users.FirstOrDefault(u => u.Mail == email);}
        public int? KullaniciGetir(string nickname){var user = _context.Users.FirstOrDefault(u => u.Nickname == nickname);return user?.Id;}

        public bool FlapVarMi(int id)
        {
            return _context.Flaps.Any(f => f.Id == id);
        }

        [CustomAuthorize]
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
                return RedirectToAction("Login");
            }

            // reCAPTCHA doðrulamasý
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
                HttpContext.Session.SetInt32("userId", user.Id);
                HttpContext.Session.SetString("Mail", user.Mail);

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

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GithubLogin()
        {
            var redirectUrl = Url.Action("GithubResponse", "Home");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "GitHub");
        }

        [AllowAnonymous]
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
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Route("/AddFlap")]
        public IActionResult AddFlap(Flap model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Eksik veya hatalý iþlem yaptýn.";
                return View("Message");
            }

            model.CreatedDate = DateTime.Now;
            model.UserId = (int)HttpContext.Session.GetInt32("userId");

            _context.Flaps.Add(model);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        [Route("/Flap/{Id}")]
        public IActionResult Flap(int Id)
        {
            var flapEntity = _context.Flaps
                .Include(f => f.Username)
                .FirstOrDefault(f => f.Id == Id);

            if (flapEntity == null)
            {
                ViewBag.Message = "Böyle bir Flap yok";
                return View("Message");
            }

            ViewData["Nickname"] = HttpContext.Session.GetString("nickname");

            ViewBag.AddYorum = CheckLogin();

            var detailFlap = new FlapRequest
            {
                Flap = flapEntity,
                Comments = _context.Comments
                    .Include(c => c.Username)
                    .Where(c => c.FlapId == Id)
                    .OrderByDescending(c => c.CreatedTime)
                    .ToList()
            };

            if (flapEntity.UserId == HttpContext.Session.GetInt32("userId"))
            {
                ViewBag.yetki = "full";
            }

            ViewBag.id = HttpContext.Session.GetInt32("userId");

            return View(detailFlap);
        }

        [CustomAuthorize]
        [Route("/profil/{mail}")]
        public async Task<IActionResult> Profile(string mail)
        {
            ViewData["mail"] = HttpContext.Session.GetString("mail");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Mail == mail);

            if (user == null)
            {
                ViewBag.Message = "Böyle bir kullanýcý yok!";
                return View("Message");
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Nickname = user.Nickname,
                Username = user.Username,
                Mail = user.Mail,
                ImgUrl = user.ImgUrl
            };

            bool isOwner = user.Id == HttpContext.Session.GetInt32("userId");
            ViewBag.profile = isOwner;

            var flapsQuery = _context.Flaps
                .Where(f => f.UserId == user.Id);

            if (!isOwner)
            {
                flapsQuery = flapsQuery.Where(f => f.Visibility);
            }

            var flapDtos = await flapsQuery
                .OrderByDescending(f => f.CreatedDate)
                .Select(f => new FlapDto
                {
                    Id = f.Id,
                    Detail = f.Detail,
                    Username = f.Username,
                    Nickname = f.Nickname,
                    ImgUrl = f.ImgUrl,
                    Visibility = f.Visibility,
                    CreatedDate = f.CreatedDate,
                    YorumSayisi = f.YorumSayisi
                })
                .ToListAsync();

            var profilDto = new ProfileRequest
            {
                User = userDto,
                Flaps = flapDtos
            };

            return View(profilDto);
        }

        [HttpPost]
        [Route("/addyorum")]
        public async Task<IActionResult> AddYorum(Comment model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            model.CreatedTime = DateTime.Now;
            model.UserId = (int)HttpContext.Session.GetInt32("userId");

            try
            {
                _context.Comments.Add(model);
                await _context.SaveChangesAsync();

                var FlapInfo = await (from f in _context.Flaps
                                      join u in _context.Users on f.UserId equals u.Id
                                      where f.Id == model.FlapId
                                      select new FlapInfoDto
                                      {
                                          Username = u.Username,
                                          Detail = f.Detail,
                                          Mail = u.Mail
                                      }).FirstOrDefaultAsync();

                if (FlapInfo != null)
                {
                    using var reader = new StreamReader("wwwroot/mailTemp/mailtemp.html");
                    var template = await reader.ReadToEndAsync();
                    var mailbody = template
                        .Replace("{{Username}}", FlapInfo.Username)
                        .Replace("{{FlapDetail}}", FlapInfo.Detail);

                    string subject = "Flappýnýza bildirim var";
                    await SendEmailAsync(FlapInfo.Mail, subject, mailbody);
                }

                return RedirectToAction("Flap", new { Id = model.FlapId });
            }
            catch (Exception)
            {
                return RedirectToAction("Index");
            }
        }

        [Route("/YorumSil/{Id}")]
        public IActionResult DeleteYorum(int Id, int FlapId)
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
        public IActionResult FlapSil(int Id, string nickname)
        {
            var flap = _context.Flaps.Find(Id);
            if (flap != null)
            {
                _context.Flaps.Remove(flap);
                _context.SaveChanges();
            }

            return RedirectToAction("Profile", new { nickname });
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/sifre-unuttum")]
        public IActionResult SifreUnuttum()
        {
            return View();
        }

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

        public async Task<IActionResult> PwResetLink(int userId)
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

        [CustomAuthorize]
        [HttpGet]
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

    }
}
