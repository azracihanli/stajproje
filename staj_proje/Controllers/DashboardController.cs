using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Users()
    {
        var users = new List<string> { "admin", "test", "demo" }; // örnek kullanıcılar
        return View(users);
    }
}