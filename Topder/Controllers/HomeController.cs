﻿using Microsoft.AspNetCore.Mvc;

namespace Topder.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Home", "Customer");
        }
    }
}
