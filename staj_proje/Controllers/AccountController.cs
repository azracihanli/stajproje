using Microsoft.AspNetCore.Mvc;
using staj_proje.Models;
using staj_proje.Helpers; // SecurityHelper için

namespace staj_proje.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Giriş Sayfası
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Username") != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: Giriş işlemi
        [HttpPost]
        public IActionResult Login(string kullaniciAdi, string sifre)
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.Hata = "Boş alan bırakmayınız.";
                return View();
            }

            var hashedPassword = SecurityHelper.Sha256Hash(sifre);

            var user = _context.Users
                .FirstOrDefault(u => u.Username == kullaniciAdi && u.Password == hashedPassword);

            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetInt32("UserId", user.Id);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Hata = "Geçersiz kullanıcı adı veya şifre.";
            return View();
        }

        // GET: Kayıt sayfası
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("Username") != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: Kayıt işlemi
        [HttpPost]
        public IActionResult Register(string kullaniciAdi, string sifre)
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.Hata = "Boş alan bırakmayınız.";
                return View();
            }

            // Kullanıcı adı zaten var mı kontrol et
            if (_context.Users.Any(u => u.Username == kullaniciAdi))
            {
                ViewBag.Hata = "Bu kullanıcı adı zaten kullanılıyor.";
                return View();
            }

            var hashedPassword = SecurityHelper.Sha256Hash(sifre);

            var user = new User
            {
                Username = kullaniciAdi,
                Password = hashedPassword,
                CreatedDate = DateTime.Now
            };

            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                TempData["Mesaj"] = "Kayıt başarıyla tamamlandı. Giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "Kayıt işlemi sırasında bir hata oluştu.";
                return View();
            }
        }

        // Çıkış işlemi
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: Profil görüntüleme
        public IActionResult Profile()
        {
            var username = HttpContext.Session.GetString("Username");
            if (username == null)
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }

        // POST: Profil güncelleme
        [HttpPost]
        public IActionResult Profile(string yeniKullaniciAdi, string yeniSifre)
        {
            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(yeniKullaniciAdi) || string.IsNullOrWhiteSpace(yeniSifre))
            {
                ViewBag.Hata = "Boş alan bırakmayınız.";
                return View(user);
            }

            // Aynı kullanıcı adı var mı kontrol
            if (_context.Users.Any(u => u.Username == yeniKullaniciAdi && u.Id != user.Id))
            {
                ViewBag.Hata = "Bu kullanıcı adı başka biri tarafından kullanılıyor.";
                return View(user);
            }

            try
            {
                user.Username = yeniKullaniciAdi;
                user.Password = SecurityHelper.Sha256Hash(yeniSifre);
                _context.SaveChanges();

                HttpContext.Session.SetString("Username", yeniKullaniciAdi);

                TempData["Mesaj"] = "Profil başarıyla güncellendi.";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "Profil güncellenirken bir hata oluştu.";
                return View(user);
            }
        }

        // GET: Tüm kullanıcıları listele
        public IActionResult AllUsers()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login");

            var users = _context.Users.OrderBy(u => u.Username).ToList();
            return View(users ?? new List<User>());
        }

        // POST: Kullanıcı sil
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login");

            var user = _context.Users.Find(id);
            var currentUsername = HttpContext.Session.GetString("Username");

            if (user != null && user.Username != currentUsername)
            {
                try
                {
                    _context.Users.Remove(user);
                    _context.SaveChanges();
                    TempData["Mesaj"] = "Kullanıcı başarıyla silindi.";
                }
                catch (Exception ex)
                {
                    TempData["Hata"] = "Kullanıcı silinirken bir hata oluştu.";
                }
            }
            else if (user?.Username == currentUsername)
            {
                TempData["Hata"] = "Kendi hesabınızı silemezsiniz.";
            }

            return RedirectToAction("AllUsers");
        }

        // GET: Kullanıcı düzenleme sayfası
        public IActionResult EditUser(int id)
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login");

            var user = _context.Users.Find(id);
            if (user == null)
                return RedirectToAction("AllUsers");

            return View(user);
        }

        // POST: Kullanıcı düzenleme
        [HttpPost]
        public IActionResult EditUser(int id, string kullaniciAdi, string sifre)
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login");

            var user = _context.Users.Find(id);
            if (user == null)
                return RedirectToAction("AllUsers");

            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.Hata = "Boş alan bırakmayınız.";
                return View(user);
            }

            // Aynı kullanıcı adı var mı kontrol
            if (_context.Users.Any(u => u.Username == kullaniciAdi && u.Id != user.Id))
            {
                ViewBag.Hata = "Bu kullanıcı adı başka biri tarafından kullanılıyor.";
                return View(user);
            }

            try
            {
                user.Username = kullaniciAdi;
                user.Password = SecurityHelper.Sha256Hash(sifre);
                _context.SaveChanges();

                TempData["Mesaj"] = "Kullanıcı başarıyla güncellendi.";
                return RedirectToAction("AllUsers");
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "Kullanıcı güncellenirken bir hata oluştu.";
                return View(user);
            }
        }
    }
}