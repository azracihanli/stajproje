using System.Text;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staj_proje.Helpers;
using staj_proje.Models;
using staj_proje.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System;

namespace staj_proje.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsUserLoggedIn() => HttpContext.Session.GetString("UserId") != null;

        private IActionResult RedirectIfNotLoggedIn()
        {
            if (!IsUserLoggedIn())
                return Redirect("/Account/Login");
            return null;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            return View();
        }

        // POST: Ana sayfa (Python entegrasyonu için)
        [HttpPost]
        public IActionResult Index(string ad)
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            ViewBag.ShowMenu = true;
            ViewBag.Username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrWhiteSpace(ad))
            {
                ViewBag.Mesaj = "Lütfen geçerli bir ad girin!";
                return View();
            }

            var psi = new ProcessStartInfo
            {
                FileName = "python", // Bilgisayarda python komutu çalışıyor olmalı
                Arguments = $"\"C:\\Users\\USER\\Documents\\staj\\staj.py\" \"{ad}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            try
            {
                var process = Process.Start(psi);
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    ViewBag.Mesaj = "Python hatası: " + error;
                }
                else
                {
                    ViewBag.Mesaj = output;
                }
            }
            catch (System.Exception ex)
            {
                ViewBag.Mesaj = "Python çalıştırılırken hata oluştu: " + ex.Message;
            }

            return View();
        }

        public async Task<IActionResult> AllRecords()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var users = await _context.Users.OrderBy(u => u.Id).ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User model)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("FullName", user.FullName);
            TempData["Success"] = "Profil bilgileriniz başarıyla güncellendi.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid) return RedirectToAction("Profile");

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var user = await _context.Users.FindAsync(userId);

            if (user != null && PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Şifreniz başarıyla değiştirildi.";
            }
            else
            {
                TempData["Error"] = "Mevcut şifreniz hatalı.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(RegisterViewModel model)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser == null)
                {
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
                    TempData["Success"] = "Kullanıcı başarıyla eklendi.";
                }
                else
                {
                    TempData["Error"] = "Bu kullanıcı adı veya email zaten kullanılıyor.";
                }
            }

            return RedirectToAction("AllRecords");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserPassword(int UserId, string NewPassword, string ConfirmNewPassword)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            if (NewPassword != ConfirmNewPassword)
            {
                TempData["Error"] = "Şifreler eşleşmiyor.";
                return RedirectToAction("AllRecords");
            }

            if (NewPassword.Length < 6)
            {
                TempData["Error"] = "Şifre en az 6 karakter olmalı.";
                return RedirectToAction("AllRecords");
            }

            var user = await _context.Users.FindAsync(UserId);
            if (user != null)
            {
                user.PasswordHash = PasswordHelper.HashPassword(NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kullanıcının şifresi başarıyla değiştirildi.";
            }

            return RedirectToAction("AllRecords");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(User model)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.Id);
                if (user != null)
                {
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Email = model.Email;

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
                }
            }

            return RedirectToAction("AllRecords");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kullanıcı başarıyla silindi.";
            }

            return RedirectToAction("AllRecords");
        }

        public IActionResult ExportToExcel()
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var users = _context.Users.OrderBy(u => u.Id).ToList();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Kullanicilar");

                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Kullanıcı Adı";
                worksheet.Cell(1, 3).Value = "Kayıt Tarihi";

                worksheet.Range("A1:C1").Style.Font.Bold = true;
                worksheet.Range("A1:C1").Style.Fill.BackgroundColor = XLColor.LightGray;

                for (int i = 0; i < users.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = users[i].Id;
                    worksheet.Cell(i + 2, 2).Value = users[i].Username;
                    worksheet.Cell(i + 2, 3).Value = users[i].CreatedDate.ToString("dd.MM.yyyy HH:mm");
                }

                worksheet.Columns().AdjustToContents();
                var tableRange = worksheet.Range($"A1:C{users.Count + 1}");
                tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0; // önemli
                var content = stream.ToArray();
                var fileName = $"Kullanicilar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Excel dosyası oluşturulurken hata: " + ex.Message;
                return RedirectToAction("AllRecords");
            }
        }

        public async Task<IActionResult> ExportToPdf()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var users = await _context.Users.OrderBy(u => u.Id).ToListAsync();

            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, stream);
            document.Open();

            var baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, BaseFont.NOT_EMBEDDED);
            var titleFont = new Font(baseFont, 18, Font.BOLD);
            var headerFont = new Font(baseFont, 12, Font.BOLD);
            var cellFont = new Font(baseFont, 10, Font.NORMAL);

            var title = new Paragraph("Kullanıcı Listesi", titleFont) { Alignment = Element.ALIGN_CENTER };
            document.Add(title);
            document.Add(new Paragraph(" "));

            var table = new PdfPTable(6) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 1, 2, 2, 2, 3, 2 });

            table.AddCell(new PdfPCell(new Phrase("ID", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Kullanıcı Adı", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Ad", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Soyad", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Email", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Kayıt Tarihi", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });

            foreach (var user in users)
            {
                table.AddCell(new PdfPCell(new Phrase(user.Id.ToString(), cellFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase(user.Username, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(user.FirstName, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(user.LastName, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(user.Email, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(user.CreatedDate.ToString("dd.MM.yyyy"), cellFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            }

            document.Add(table);
            document.Close();

            stream.Position = 0; // önemli
            return File(stream.ToArray(), "application/pdf", "Kullanicilar.pdf");
        }
    }
}
