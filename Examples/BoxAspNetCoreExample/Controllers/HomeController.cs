using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BoxAspNetCore.Models;
using Box.AspNetCore.Integration;

namespace BoxAspNetCore.Controllers
{
    public class HomeController : Controller
    {
        IBoxPlatformService _box;

        public HomeController(IBoxPlatformService boxService)
        {
            _box = boxService;
        }

        public async Task<IActionResult> Index()
        {
            var adminClient = _box.AdminClient();
            var adminUser = await adminClient.UsersManager.GetCurrentUserInformationAsync();
            ViewData["BoxAdminName"] = adminUser.Name;

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
