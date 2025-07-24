using System.Text;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staj_proje.Helpers;
using staj_proje.Models;
using staj_proje.ViewModels;

namespace staj_proje.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsUserLoggedIn()
        {
            return HttpContext.Session.GetString("UserId") != null;
        }

        private void RedirectIfNotLoggedIn()
        {
            if (!IsUserLoggedIn())
            {
                Response.Redirect("/Account/Login");
            }
        }

        public IActionResult Index()
        {
            RedirectIfNotLoggedIn();
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            return View();
        }

        public async Task<IActionResult> AllRecords()
        {
            RedirectIfNotLoggedIn();
            var users = await _context.Users.OrderBy(u => u.Id).ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            RedirectIfNotLoggedIn();

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User model)
        {
            RedirectIfNotLoggedIn();

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Sadece güncellenmesi gereken alanlarý güncelle
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Session'daki FullName'i güncelle
            HttpContext.Session.SetString("FullName", user.FullName);

            TempData["Success"] = "Profil bilgileriniz baþarýyla güncellendi.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            RedirectIfNotLoggedIn();

            if (ModelState.IsValid)
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId"));
                var user = await _context.Users.FindAsync(userId);

                if (user != null && PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Þifreniz baþarýyla deðiþtirildi.";
                    return RedirectToAction("Profile");
                }
                else
                {
                    TempData["Error"] = "Mevcut þifreniz hatalý.";
                }
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(RegisterViewModel model)
        {
            RedirectIfNotLoggedIn();

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

                    TempData["Success"] = "Kullanýcý baþarýyla eklendi.";
                }
                else
                {
                    TempData["Error"] = "Bu kullanýcý adý veya email zaten kullanýlýyor.";
                }
            }

            return RedirectToAction("AllRecords");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserPassword(int UserId, string NewPassword, string ConfirmNewPassword)
        {
            RedirectIfNotLoggedIn();

            if (NewPassword != ConfirmNewPassword)
            {
                TempData["Error"] = "Þifreler eþleþmiyor.";
                return RedirectToAction("AllRecords");
            }

            if (NewPassword.Length < 6)
            {
                TempData["Error"] = "Þifre en az 6 karakter olmalý.";
                return RedirectToAction("AllRecords");
            }

            var user = await _context.Users.FindAsync(UserId);
            if (user != null)
            {
                user.PasswordHash = PasswordHelper.HashPassword(NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Kullanýcýnýn þifresi baþarýyla deðiþtirildi.";
            }

            return RedirectToAction("AllRecords");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(User model)
        {
            RedirectIfNotLoggedIn();

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

                    TempData["Success"] = "Kullanýcý baþarýyla güncellendi.";
                }
            }

            return RedirectToAction("AllRecords");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            RedirectIfNotLoggedIn();

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kullanýcý baþarýyla silindi.";
            }

            return RedirectToAction("AllRecords");
        }

        public IActionResult ExportToExcel()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            try
            {
                // ID'ye göre sýralama yaparak tutarlýlýðý saðla
                var users = _context.Users.OrderBy(u => u.Id).ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Kullanicilar");

                    // Baþlýklar
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Kullanýcý Adý";
                    worksheet.Cell(1, 3).Value = "Kayýt Tarihi";

                    // Baþlýk satýrýný kalýn yap
                    worksheet.Range("A1:C1").Style.Font.Bold = true;
                    worksheet.Range("A1:C1").Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Veriler
                    for (int i = 0; i < users.Count; i++)
                    {
                        worksheet.Cell(i + 2, 1).Value = users[i].Id;
                        worksheet.Cell(i + 2, 2).Value = users[i].Username;
                        worksheet.Cell(i + 2, 3).Value = users[i].CreatedDate.ToString("dd.MM.yyyy HH:mm");
                    }

                    // Otomatik sütun geniþliði
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
                TempData["Hata"] = "Excel dosyasý oluþturulurken hata: " + ex.Message;
                return RedirectToAction("AllUsers");
            }
        }


        public async Task<IActionResult> ExportToPdf()
        {
            RedirectIfNotLoggedIn();

            var users = await _context.Users.OrderBy(u => u.Id).ToListAsync();

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 25, 25, 30, 30);
                var writer = PdfWriter.GetInstance(document, stream);
                document.Open();

                // Türkçe karakter desteði için font
                var baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, BaseFont.NOT_EMBEDDED);
                var titleFont = new Font(baseFont, 18, Font.BOLD);
                var headerFont = new Font(baseFont, 12, Font.BOLD);
                var cellFont = new Font(baseFont, 10, Font.NORMAL);

                // Baþlýk
                var title = new Paragraph("Kullanýcý Listesi", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);
                document.Add(new Paragraph(" "));

                // Tablo
                var table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1, 2, 2, 2, 3, 2 });

                // Baþlýk satýrý
                table.AddCell(new PdfPCell(new Phrase("ID", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Kullanýcý Adý", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Ad", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Soyad", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Email", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Kayýt Tarihi", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });

                // Veri satýrlarý
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

                return File(stream.ToArray(), "application/pdf", "Kullanicilar.pdf");
            }
        }
    }
}