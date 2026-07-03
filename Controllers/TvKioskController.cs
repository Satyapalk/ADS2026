using Microsoft.AspNetCore.Mvc;

namespace ADS2026.Controllers
{
    public class TvKioskController : Controller
    {
        [HttpGet("/tv")]
        public IActionResult Index(string screen = "all")
        {
            ViewBag.Screen = screen;
            return View("~/Views/TV/Index.cshtml");
        }
    }
}