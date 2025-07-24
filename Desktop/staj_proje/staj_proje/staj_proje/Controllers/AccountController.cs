using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staj_proje.Helpers;
using staj_proje.Models;
using staj_proje.ViewModels;
using System.IO;

namespace staj_proje.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user != null && PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

            if (existingUser != null)
            {
                if (existingUser.Username == model.Username)
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor.");

                if (existingUser.Email == model.Email)
                    ModelState.AddModelError("Email", "Bu email adresi zaten kullanılıyor.");

                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ExportToExcel()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            try
            {
                // ID'ye göre sıralama yaparak tutarlılığı sağla
                var users = _context.Users.OrderBy(u => u.Id).ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Kullanicilar");

                    // Başlıklar
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Kullanıcı Adı";
                    worksheet.Cell(1, 3).Value = "Kayıt Tarihi";

                    // Başlık satırını kalın yap
                    worksheet.Range("A1:C1").Style.Font.Bold = true;
                    worksheet.Range("A1:C1").Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Veriler
                    for (int i = 0; i < users.Count; i++)
                    {
                        worksheet.Cell(i + 2, 1).Value = users[i].Id;
                        worksheet.Cell(i + 2, 2).Value = users[i].Username;
                        worksheet.Cell(i + 2, 3).Value = users[i].CreatedDate.ToString("dd.MM.yyyy HH:mm");
                    }

                    // Otomatik sütun genişliği
                    worksheet.Columns().AdjustToContents();

                    // Tablo stilini uygula
                    var tableRange = worksheet.Range($"A1:C{users.Count + 1}");
                    tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        var fileName = $"Kullanicilar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Excel dosyası oluşturulurken hata: " + ex.Message;
                return RedirectToAction("AllUsers");
            }
        }

    }
}
