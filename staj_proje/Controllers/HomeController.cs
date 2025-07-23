using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using staj_proje.Models;
using staj_proje.Helpers;
using System.Linq;

namespace staj_proje.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // Ana sayfa
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            ViewBag.ShowMenu = true;
            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }

        // Profil görüntüleme
        [HttpGet]
        public IActionResult Profile()
        {
            var username = HttpContext.Session.GetString("Username");
            if (username == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.ShowMenu = true;
            return View(user);
        }

        // Profil güncelleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(string yeniKullaniciAdi, string eskiSifre, string yeniSifre)
        {
            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.ShowMenu = true;

            // Validasyon kontrolleri
            if (string.IsNullOrWhiteSpace(yeniKullaniciAdi) ||
                string.IsNullOrWhiteSpace(eskiSifre) ||
                string.IsNullOrWhiteSpace(yeniSifre))
            {
                ViewBag.Hata = "Tüm alanlarý doldurunuz.";
                return View(user);
            }

            // Eski þifre doðru mu kontrol et
            var hashedEskiSifre = SecurityHelper.Sha256Hash(eskiSifre);
            if (user.Password != hashedEskiSifre)
            {
                ViewBag.Hata = "Mevcut þifreniz yanlýþ.";
                return View(user);
            }

            // Yeni kullanýcý adý zaten var mý kontrol et
            if (_context.Users.Any(u => u.Username == yeniKullaniciAdi && u.Id != user.Id))
            {
                ViewBag.Hata = "Bu kullanýcý adý baþka biri tarafýndan kullanýlýyor.";
                return View(user);
            }

            try
            {
                // Güncelleme iþlemi
                user.Username = yeniKullaniciAdi;
                user.Password = SecurityHelper.Sha256Hash(yeniSifre);
                _context.SaveChanges();

                // Session'ý güncelle
                HttpContext.Session.SetString("Username", yeniKullaniciAdi);

                TempData["Basarili"] = "Profil baþarýyla güncellendi.";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "Profil güncellenirken bir hata oluþtu.";
                return View(user);
            }
        }

        // Tüm kullanýcýlarý listele
        public IActionResult AllUsers()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            var users = _context.Users.OrderBy(u => u.Username).ToList();
            ViewBag.ShowMenu = true;
            return View(users ?? new List<User>());
        }

        // Kullanýcý silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            var currentUsername = HttpContext.Session.GetString("Username");

            if (user != null && user.Username != currentUsername)
            {
                try
                {
                    _context.Users.Remove(user);
                    _context.SaveChanges();
                    TempData["Basarili"] = "Kullanýcý baþarýyla silindi.";
                }
                catch (Exception ex)
                {
                    TempData["Hata"] = "Kullanýcý silinirken bir hata oluþtu.";
                }
            }
            else if (user?.Username == currentUsername)
            {
                TempData["Hata"] = "Kendi hesabýnýzý silemezsiniz.";
            }

            return RedirectToAction("AllUsers");
        }

        // Kullanýcý düzenleme sayfasý
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null)
                return RedirectToAction("AllUsers");

            ViewBag.ShowMenu = true;
            return View(user);
        }

        // Kullanýcý düzenleme iþlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int id, string kullaniciAdi, string sifre)
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null)
                return RedirectToAction("AllUsers");

            ViewBag.ShowMenu = true;

            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.Hata = "Boþ alan býrakmayýnýz.";
                return View(user);
            }

            // Ayný kullanýcý adý var mý kontrol
            if (_context.Users.Any(u => u.Username == kullaniciAdi && u.Id != user.Id))
            {
                ViewBag.Hata = "Bu kullanýcý adý baþka biri tarafýndan kullanýlýyor.";
                return View(user);
            }

            try
            {
                user.Username = kullaniciAdi;
                user.Password = SecurityHelper.Sha256Hash(sifre);
                _context.SaveChanges();

                TempData["Basarili"] = "Kullanýcý baþarýyla güncellendi.";
                return RedirectToAction("AllUsers");
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "Kullanýcý güncellenirken bir hata oluþtu.";
                return View(user);
            }
        }

        // Çýkýþ yapma
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}