using Microsoft.AspNetCore.Mvc;

namespace ADS2026.Controllers;

public class DisplayScreenController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}