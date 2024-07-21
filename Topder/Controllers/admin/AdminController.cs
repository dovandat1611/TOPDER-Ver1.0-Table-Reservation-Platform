using Microsoft.AspNetCore.Mvc;
using static PROJECT_PRN221.Utils.Mail;
using Topder.Models;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using Topder.Utils;
using Microsoft.Extensions.Options;
using PROJECT_PRN221.Utils;
using Newtonsoft.Json;
using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Components.Forms;
using Topder.Dto;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Reflection.Metadata;

namespace Topder.Controllers.admin
{
    [Route("admin")]
    public class AdminController : Controller
    {

        private readonly TopderContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly MailSettings mailSettings;
      //  Password = "zhsb xtgm aziv vqnb",
        public AdminController(TopderContext context, IWebHostEnvironment environment)
        {
            _context = context;
            mailSettings = new MailSettings()
            {
                Mail = "topder.vn@gmail.com",
                DisplayName = "Topder",
                Password = "gusw sedp tpoa siak",
                Host = "smtp.gmail.com",
                Port = 587
            };
            _environment = environment;
        }

        public bool checkSession()
        {
            bool checkS = true;
            var httpContext = HttpContext;
            if (httpContext != null && httpContext.Session != null)
            {
                string isCustomerAuthenticated = httpContext.Session.GetString("admin");
                if (string.IsNullOrEmpty(isCustomerAuthenticated))
                {
                    checkS = false;
                }
            }
            return checkS;
        }

        [HttpGet("dashboard")]
        public IActionResult Dashboard(string service, int year)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            // Task Bar
            ViewBag.TotalOrder = _context.Orders.Count();
            ViewBag.TotalCustomer = _context.Customers.Count();
            ViewBag.TotalRestaurant = _context.Restaurants.Count();
            ViewBag.TotalBlog = _context.BlogGroups.Count();
            ViewBag.TotalContact = _context.Contacts.Count();

            // ORDER STATUS
            ViewBag.Wait = _context.Orders.Where(x => x.Statusorder.Equals("Wait")).Count();
            ViewBag.Accept = _context.Orders.Where(x => x.Statusorder.Equals("Accept")).Count();
            ViewBag.Process = _context.Orders.Where(x => x.Statusorder.Equals("Process")).Count();
            ViewBag.Done = _context.Orders.Where(x => x.Statusorder.Equals("Done")).Count();
            ViewBag.Cancel = _context.Orders.Where(x => x.Statusorder.Equals("Cancel")).Count();


            // TopRestaurant 
            List<TopRestaurant> topRestaurant = _context.Orders
                                        .Join(_context.Restaurants,
                                        o => o.RestaurantId,
                                        r => r.ResId,
                                        (o, r) => new { o, r })
                                        .GroupBy(x => x.r.ResId)
                                        .Select(g => new TopRestaurant()
                                        {
                                            RestaurantId = g.Key,
                                            TotalOrder = g.Count(),
                                            Name = g.FirstOrDefault().r.NameRes,
                                            Image = g.FirstOrDefault().r.Image
                                        })
                                        .OrderByDescending(x => x.TotalOrder)
                                        .Take(5)
                                        .ToList();

            // SEARCH TOTAL Order YEAR
            if (service == null)
            {
                service = "TotalOrderCurrentYear";
            }

            int yearinchart = 0;

            if (service.Equals("TotalOrderCurrentYear"))
            {
                ViewBag.Year = DateTime.Now.Year;
                yearinchart = DateTime.Now.Year;
                ViewBag.TotalOrderYear = _context.Orders
                .Where(x => x.CreateDate.Value.Year == DateTime.Now.Year)
                .Count();
            }
            if (service.Equals("searchOrderYear"))
            {
                ViewBag.Year = year;
                yearinchart = year;
                ViewBag.TotalOrderYear = _context.Orders
                .Where(x => x.CreateDate.Value.Year == year)
                .Count();
            }

            // CHART TOTAL INCOME YEAR
            ViewBag.t1 = getChartTotalOrderForYear(1, yearinchart);
            ViewBag.t2 = getChartTotalOrderForYear(2, yearinchart);
            ViewBag.t3 = getChartTotalOrderForYear(3, yearinchart);
            ViewBag.t4 = getChartTotalOrderForYear(4, yearinchart);
            ViewBag.t5 = getChartTotalOrderForYear(5, yearinchart);
            ViewBag.t6 = getChartTotalOrderForYear(6, yearinchart);
            ViewBag.t7 = getChartTotalOrderForYear(7, yearinchart);
            ViewBag.t8 = getChartTotalOrderForYear(8, yearinchart);
            ViewBag.t9 = getChartTotalOrderForYear(9, yearinchart);
            ViewBag.t10 = getChartTotalOrderForYear(10, yearinchart);
            ViewBag.t11 = getChartTotalOrderForYear(11, yearinchart);
            ViewBag.t12 = getChartTotalOrderForYear(12, yearinchart);

            List<TopCategories> categoryCount = _context.Categories
              .Select(c => new TopCategories
              {
                  CategoryId = c.CategoryId,
                  CategoryName = c.CategoryName,
                  RestaurantCount = c.Restaurants.Count
              })
              .OrderByDescending(c => c.RestaurantCount)
              .ToList();

            foreach (var category in categoryCount)
            {
                string decodedCategoryName = WebUtility.HtmlDecode(category.CategoryName);
                Console.WriteLine(decodedCategoryName);
            }
            ViewBag.TopCategories = categoryCount;

            return View(topRestaurant);
        }

        private int getChartTotalOrderForYear(int mounth, int year)
        {
            int totalOrder = _context.Orders
            .Where(o => o.CreateDate.Value.Month == mounth && o.CreateDate.Value.Year == year)
            .Count();
            return totalOrder;
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            var session = HttpContext.Session;

            if (session.Keys.Contains("admin"))
            {
                session.Remove("admin");
            }
            return RedirectToAction("Login");
        }

        [HttpGet("authenticate/login")]
        public IActionResult Login()
        {   
            return View();
        }

        [HttpPost("authenticate/login")]
        public IActionResult Login(string username, string password)
        {   
            if (_context.Admins != null)
            {
                if (username == string.Empty || password == string.Empty)
                {
                    ViewBag.ErrorMess = "Tên đăng nhập hoặc mật khẩu không được bỏ trống.";
                    return View();
                }
                else
                {
                    Admin admin = _context.Admins.FirstOrDefault(x => x.Username.Equals(username) && x.Password.Equals(password));
                    if (admin != null)
                    {
                        if (admin.Status.Equals("Deactive"))
                        {
                            ViewBag.ErrorMess = "Tài khoản đang bị vô hiệu hóa.";
                            return View();
                        }
                        string adminJson = JsonConvert.SerializeObject(admin);
                        HttpContext.Session.SetString("admin", adminJson);
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        ViewBag.ErrorMess = "Tên đăng nhập hoặc mật khẩu sai.";
                        return View();
                    }
                }
            }
            return View();
        }

        [HttpGet("authenticate/forgotpassword")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost("authenticate/forgotpassword")]
        public IActionResult ForgotPassword(string username, string email)
        {
            Admin admin = _context.Admins.FirstOrDefault(x => x.Username.Equals(username) && x.Email.Equals(email));

            if (admin != null)
            {
                string otp = Validation.GenerateOTP(6);
                HttpContext.Session.SetString("otp", otp);
                HttpContext.Session.SetString("username", username);
                
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger<SendMailService>();
                Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                sendMailService.SendEmailAsync(email, "OTP", otp);

                return RedirectToAction("OTP");
            }
            else
            {
                ViewBag.ErrorMess = "Tên đăng nhập hoặc email sai.";
                return View();
            }
        }

        [HttpGet("authenticate/otp")]
        public IActionResult OTP()
        {
            return View();
        }

        [HttpPost("authenticate/otp")]
        public IActionResult OTP(string otp)
        {
            string sessionOTP = HttpContext.Session.GetString("otp");

            if (!string.IsNullOrEmpty(sessionOTP))
            {
                if (sessionOTP.Equals(otp))
                {
                    return RedirectToAction("ResetPassword");
                }
                else
                {
                    HttpContext.Session.Remove("username");
                    HttpContext.Session.Remove("otp");
                    return RedirectToAction("ForgotPassword");
                }
            }
            return RedirectToAction("ForgotPassword");
        }


        [HttpGet("authenticate/resetpassword")]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost("authenticate/resetpassword")]
        public IActionResult ResetPassword(string pass, string repass)
        {
            string username = HttpContext.Session.GetString("username");

            if (pass.Equals(repass) && Utils.Validation.IsPasswordValid(pass))
            {
                Admin admin = _context.Admins.FirstOrDefault(x => x.Username.Equals(username));
                admin.Password = pass;
                _context.Admins.Update(admin);
                _context.SaveChanges();
                HttpContext.Session.Remove("username");
                HttpContext.Session.Remove("otp");
                return RedirectToAction("Login");
            }
            else
            {
                if (!Utils.Validation.IsPasswordValid(pass))
                {
                    ViewBag.PasswordError = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ thường, chữ hoa và một số.";
                }
                if (pass != repass)
                {
                    ViewBag.RepasswordError = "Mật khẩu và nhập lại mật khẩu không khớp.";
                }
                return View();
            }
        }

        [HttpGet("changespassword")]
        public IActionResult ChangesPassword()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            string adminJson = HttpContext.Session.GetString("admin");
            if (!string.IsNullOrEmpty(adminJson))
            {
                Admin adminSon = JsonConvert.DeserializeObject<Admin>(adminJson);
                Admin admin = _context.Admins.FirstOrDefault(x => x.AdminId == adminSon.AdminId);
                return View(admin);
            }
            return View();
        }

        [HttpPost("changespassword")]
        public IActionResult ChangesPassword(string spass, string respass)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            string adminJson = HttpContext.Session.GetString("admin");
            bool checkInput = true;
            if (!string.IsNullOrEmpty(adminJson))
            {
                Admin adminSon = JsonConvert.DeserializeObject<Admin>(adminJson);
                Admin admin = _context.Admins.FirstOrDefault(x => x.AdminId == adminSon.AdminId);

                if (!Utils.Validation.IsPasswordValid(spass))
                {
                    ViewBag.PasswordError = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ thường, chữ hoa và một số.";
                    checkInput = false;
                }

                if (!spass.Equals(respass))
                {
                    ViewBag.RepasswordError = "Mật khẩu và nhập lại mật khẩu không khớp.";
                    checkInput = false;
                }
                if (checkInput == true)
                {
                        admin.Password = spass;
                        _context.Admins.Update(admin);
                        if (_context.SaveChanges() > 0)
                        {
                            ViewBag.MessSuccess = "Đổi mật khẩu thành công!";
                            return View(admin);
                        }
                    ViewBag.MessError = "Đổi mật khẩu không thành công.";
                    return View(admin);
                }
                return View(admin);
            }
            return RedirectToAction("Login");
        }


        [HttpGet("profile")]
        public IActionResult Profile()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string adminJson = HttpContext.Session.GetString("admin");
            if (!string.IsNullOrEmpty(adminJson))
            {
                Admin adminConvert = JsonConvert.DeserializeObject<Admin>(adminJson);
                id = adminConvert.AdminId;
            }
            Admin admin = _context.Admins.FirstOrDefault(x => x.AdminId == id);
            return View(admin);
        }

        [HttpGet("profile/edit")]
        public async Task<IActionResult> EditProfile()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;

            string adminJson = HttpContext.Session.GetString("admin");

            if (!string.IsNullOrEmpty(adminJson))
            {
                Admin adminConvert = JsonConvert.DeserializeObject<Admin>(adminJson);
                id = adminConvert.AdminId;
            }

            if (id == 0 || _context.Admins == null)
            {
                return NotFound();
            }

            Admin admin = await _context.Admins.FirstOrDefaultAsync(x => x.AdminId == id);

            if (admin == null)
            {
                return NotFound();
            }
            return View(admin);
        }

        [HttpPost("profile/edit")]
        public async Task<IActionResult> EditProfile(string name, string phone, string email, DateTime dob, IFormFile fileUpload)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            string adminJson = HttpContext.Session.GetString("admin");
            bool checkInput = true;
            if (!string.IsNullOrEmpty(adminJson))
            {
                Admin adminSon = JsonConvert.DeserializeObject<Admin>(adminJson);
                Admin admin = _context.Admins.FirstOrDefault(x => x.AdminId == adminSon.AdminId);

                if(admin == null)
                {
                    return NotFound();
                }

                if (!Utils.Validation.IsEmailValid(email))
                {
                    ViewBag.EmailError = "Định dạng email không hợp lệ.";
                    checkInput = false;
                }

                if (checkInput == true)
                {   
                    admin.Name = name;
                    admin.Phone = phone;
                    admin.Email = email;
                    admin.Dob = dob;

                    //// Upload file 
                    string fileURL = string.Empty;
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images/avatar");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(fileStream);
                            fileURL = "/Images/avatar/" + uniqueFileName;
                        }
                        admin.Image = fileURL;
                    }

                    _context.Admins.Update(admin);
                    if (_context.SaveChanges() > 0)
                    {
                        return RedirectToAction("Profile");
                    }
                    ViewBag.MessError = "Chỉnh sửa hồ sơ không thành công.";
                    return View(admin);
                }
                return View(admin);
            }
            return RedirectToAction("Login");
        }

        [HttpGet("account/admin")]
        public async Task<IActionResult> Admin(int? pageIndex)
        {
            if (_context.Admins != null)
            {
                IQueryable<Admin> adminsIQ = _context.Admins;
                ViewBag.AdminListCount = _context.Admins.Count();
                int pageSize = 10;
                List<Admin> admins = await PaginatedList<Admin>.CreateAsync(
                adminsIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                return View(admins);
            }
            return BadRequest();
        }

        [HttpPost("account/admin")]
        public async Task<IActionResult> Admin(int id, string service, string status, string name, int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            List<Admin> admins = new List<Admin>();
            if (_context.Admins != null)
            {
                IQueryable<Admin> adminsIQ = null;
                if (service.Equals("updateStatus"))
                {   
                    Admin admin = _context.Admins.FirstOrDefault(x => x.AdminId == id);
                    if (!admin.Status.Equals(status))
                    {
                        admin.Status = status;
                        _context.Admins.Update(admin);
                        _context.SaveChanges();
                    }
                    adminsIQ = _context.Admins;

                }
                if (service.Equals("searchByName"))
                {
                    adminsIQ = _context.Admins.Where(x => x.Name.Contains(name));
                }

                ViewBag.AdminListCount = _context.Admins.Count();
                var pageSize = 10;
                admins = await PaginatedList<Admin>.CreateAsync(
                adminsIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                return View(admins);
            }
            return NotFound();
        }


        [HttpGet("account/admin/create")]
        public IActionResult CreateAdmin()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost("account/admin/create")]
        public async Task<IActionResult> CreateAdmin(string name, string phone, string email, DateTime dob, string username, string password, string repassword, IFormFile fileUpload)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            bool checkInput = true;

            if (!Utils.Validation.IsUsernameAdmin(username, _context))
            {
                ViewBag.UsernameError = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";
                checkInput = false;
            }

            if (!Utils.Validation.IsEmailValid(email))
            {
                ViewBag.EmailError = "Định dạng email không hợp lệ.";
                checkInput = false;
            }

            if (!Utils.Validation.IsPasswordValid(password))
            {
                ViewBag.PasswordError = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ thường, chữ hoa và một số.";
                checkInput = false;
            }

            if (!password.Equals(repassword))
            {
                ViewBag.RepasswordError = "Mật khẩu và nhập lại mật khẩu không khớp.";
                checkInput = false;
            }
            if (checkInput == true)
            {

                Admin admin = new Admin()
                {
                    Name = name,
                    Phone = phone,
                    Email = email,
                    Dob = dob,
                    Username = username,
                    Password = password,
                    Status = "Active"
                };
                //// Upload file 
                string fileURL = string.Empty;
                if (fileUpload != null && fileUpload.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images/avatar");
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileUpload.CopyToAsync(fileStream);
                        fileURL = "/Images/avatar/" + uniqueFileName;
                    }
                    admin.Image = fileURL;
                }

                await _context.Admins.AddAsync(admin);
                await _context.SaveChangesAsync();
                return RedirectToAction("Admin");
            }
            return View();
        }


        [HttpGet("account/customer")]
        public async Task<IActionResult> Customer(int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.Customers != null)
            {
                IQueryable<Customer> customersIQ = _context.Customers;
                int pageSize = 10;
                List<Customer> customers = await PaginatedList<Customer>.CreateAsync(customersIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                ViewBag.CustomersCount = _context.Customers.Count();
                return View(customers);
            }
            return NotFound();
        }

        [HttpPost("account/customer")]
        public async Task<IActionResult> Customer(int id, string service, string status, string name, int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            List<Customer> customers = new List<Customer>();
            if (_context.Customers != null)
            {
                IQueryable<Customer> customersIQ = null;
                if (service.Equals("updateStatus"))
                {
                    Customer customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);
                    if (!customer.Status.Equals(status))
                    {
                        customer.Status = status;
                        _context.Customers.Update(customer);
                        _context.SaveChanges();
                    }


                    customersIQ = _context.Customers;

                }
                if (service.Equals("searchByName"))
                {
                    customersIQ = _context.Customers.Where(x => x.Name.Contains(name));
                }

                ViewBag.CustomersCount = _context.Customers.Count();
                var pageSize = 10;
                customers = await PaginatedList<Customer>.CreateAsync(
                customersIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                return View(customers);
            }
            return NotFound();
        }

        [HttpGet("account/restaurant")]
        public async Task<IActionResult> Restaurant(int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.Restaurants != null)
            {
                IQueryable<Restaurant> restaurantsIQ = _context.Restaurants.Include(x => x.Category);
                int pageSize = 10;
                ViewBag.RestaurantCount = _context.Restaurants.Count();
                List<Restaurant> restaurants = await PaginatedList<Restaurant>.CreateAsync(restaurantsIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                ViewBag.Categories = _context.Categories.ToList();
                return View(restaurants);
            }
            return NotFound();
        }

        [HttpPost("account/restaurant")]
        public async Task<IActionResult> Restaurant(int id, int category_id, string service, string status, string name, int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            List<Restaurant> restaurants = new List<Restaurant>();
            if (_context.Restaurants != null)
            {
                ViewBag.RestaurantCount = _context.Restaurants.Count();
                IQueryable<Restaurant> restaurantsIQ = null;
                if (service.Equals("updateStatus"))
                {
                    Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);
                    if (!restaurant.Status.Equals(status))
                    {
                        restaurant.Status = status;
                        _context.Restaurants.Update(restaurant);
                        if (_context.SaveChanges() > 0)
                        {
                            if (status.Equals("Active"))
                            {
                                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                                var logger = loggerFactory.CreateLogger<SendMailService>();
                                Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                                sendMailService.SendEmailAsync(restaurant.Email, "Active Account", restaurant.NameRes);
                            }
                        }
                    }

                    restaurantsIQ = _context.Restaurants.Include(x => x.Category);

                }
                if (service.Equals("searchRestaurant"))
                {
                    restaurantsIQ = _context.Restaurants.Include(x => x.Category).Where(x => x.NameRes.Contains(name));
                }
                if(service.Equals("searchCategory"))
                {
                    restaurantsIQ = _context.Restaurants.Include(x => x.Category).Where(x => x.CategoryId == category_id);
                }

                ViewBag.Categories = _context.Categories.ToList();

                var pageSize = 10;
                restaurants = await PaginatedList<Restaurant>.CreateAsync(
                restaurantsIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                return View(restaurants);
            }
            return NotFound();
        }

        [HttpGet("blog/preview")]
        public async Task<IActionResult> BlogPreview(int ?id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (id == 0 || id == null || _context.Blogs == null)
            {
                return BadRequest();
            }
            else
            {
                Blog blog = _context.Blogs.Include(x => x.Admin).Include(x => x.Bloggroup).FirstOrDefault(x => x.BlogId == id);

                List<Blog> blogs = _context.Blogs.Include(x => x.Admin).Include(x => x.Bloggroup).Where(x => x.BlogId != id)
                    .OrderByDescending(x => x.BlogId).Take(3).ToList();

                ViewBag.NewBlog = blogs;
                ViewBag.NewBlogCount = blogs.Count();
                ViewBag.BlogGroups = _context.BlogGroups.ToList();

                return View(blog);
            }
        }

        [HttpGet("blog")]
        public async Task<IActionResult>  Blog(int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.Blogs != null)
            {
                IQueryable<Blog> blogsIQ = _context.Blogs.Include(n => n.Admin).Include(n => n.Bloggroup);
                int pageSize = 10;
                PaginatedList<Blog> blogs = await PaginatedList<Blog>.CreateAsync(
                blogsIQ.AsNoTracking(), pageIndex ?? 1, pageSize);

                ViewData["BlogGroups"] = new SelectList(_context.BlogGroups, "BloggroupId", "BloggroupName");
                ViewBag.BlogCount = _context.Blogs.Count();
                ViewBag.BlogGroupsCount = _context.BlogGroups.Count();
                View(blogs);
            }
            return View();
        }

        [HttpPost("blog")]
        public async Task<IActionResult> Blog(int id, string status,string service, string title, int blogsgroup_id, int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.Blogs != null || _context.BlogGroups != null)
            {
                IQueryable<Blog> blogsQuery = _context.Blogs
                    .Include(n => n.Admin)
                    .Include(n => n.Bloggroup);

                ViewData["BlogGroups"] = new SelectList(_context.BlogGroups, "BloggroupId", "BloggroupName");

                if (service.Equals("updateStatus"))
                {
                    Blog blog = _context.Blogs.FirstOrDefault(x => x.BlogId == id);
                    if (!blog.Status.Equals(status))
                    {
                        blog.Status = status;
                        _context.Blogs.Update(blog);
                        _context.SaveChanges();
                    }
                }
                if (service.Equals("searchGroup"))
                {
                    blogsQuery = blogsQuery.Where(x => x.BloggroupId == blogsgroup_id);
                }

                if (service.Equals("searchBlog"))
                {
                    if (blogsgroup_id == 0)
                    {
                        blogsQuery = blogsQuery.Where(x => x.Title.Contains(title));
                    }
                    else
                    {
                        blogsQuery = blogsQuery.Where(x => x.Title.Contains(title) && x.BloggroupId == blogsgroup_id);
                    }
                }

                ViewBag.BlogGroupsId = blogsgroup_id != 0 ? blogsgroup_id : 0;
                ViewBag.BlogCount = _context.Blogs.Count();
                ViewBag.BlogGroupsCount = _context.BlogGroups.Count();
                int pageSize = 10;
                PaginatedList<Blog> blogs = await PaginatedList<Blog>.CreateAsync(
                blogsQuery.AsNoTracking(), pageIndex ?? 1, pageSize);
                return View(blogs);
            }
            return NotFound();
        }

        [HttpGet("blog/create")]
        public IActionResult CreateBlog()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            ViewData["BlogGroups"] = new SelectList(_context.BlogGroups, "BloggroupId", "BloggroupName");
            return View();
        }


        [HttpPost("blog/create")]
        public async Task<IActionResult>  CreateBlog(int bloggroup_id,string title, string content, IFormFile fileUpload)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.Blogs != null || _context.BlogGroups != null)
            {
                ViewData["BlogGroups"] = new SelectList(_context.BlogGroups, "BloggroupId", "BloggroupName");
                Admin admin = null;
                string adminJson = HttpContext.Session.GetString("admin");
                if (!string.IsNullOrEmpty(adminJson))
                {
                    admin = JsonConvert.DeserializeObject<Admin>(adminJson);
                }

                if(admin != null)
                {
                    Blog blog = new Blog()
                {
                    Content = content,
                    Title = title,
                    BloggroupId = bloggroup_id,
                    AdminId = admin.AdminId,
                    CreateDate = DateTime.Now,
                    Status = "Deactive",
                };
                    //// Upload file 
                    string fileURL = string.Empty;
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images/blog");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(fileStream);
                            fileURL = "/Images/blog/" + uniqueFileName;
                        }
                        blog.Image = fileURL;
                    }
                    await _context.Blogs.AddAsync(blog);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Blog");
                }
                return NotFound();
            }
            return BadRequest();
        }



        [HttpGet("blog/edit")]
        public async Task<IActionResult> EditBlog(int? id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (id == null || _context.Blogs == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs.FirstOrDefaultAsync(m => m.BlogId == id);
            if (blog == null)
            {
                return NotFound();
            }
            Blog blogmodel = blog;
            ViewBag.BlogGroups = _context.BlogGroups.ToList();
            return View(blogmodel);
        }

        [HttpPost("blog/edit")]
        public async Task<IActionResult> EditBlog(int id, int bloggroup_id, string title, string content, IFormFile fileUpload)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.Blogs != null || _context.BlogGroups != null)
            {
                ViewData["BlogGroups"] = new SelectList(_context.BlogGroups, "BloggroupId", "BloggroupName");
                Admin admin = null;
                string adminJson = HttpContext.Session.GetString("admin");
                if (!string.IsNullOrEmpty(adminJson))
                {
                    admin = JsonConvert.DeserializeObject<Admin>(adminJson);
                }

                if (admin != null)
                {
                    Blog blog = _context.Blogs.FirstOrDefault(x => x.BlogId == id);

                    blog.Content = content;
                    blog.Title = title;
                    blog.BloggroupId = bloggroup_id;

                    //// Upload file 
                    string fileURL = string.Empty;
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images/blog");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(fileStream);
                            fileURL = "/Images/blog/" + uniqueFileName;
                        }
                        blog.Image = fileURL;
                    }
                    _context.Blogs.Update(blog);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Blog");
                }
                return NotFound();
            }
            return BadRequest();
        }

        [HttpGet("bloggroup")]
        public async Task<IActionResult> BlogGroup(int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.BlogGroups != null)
            {
                IQueryable<BlogGroup> IQ = _context.BlogGroups;
                int pageSize = 10;
                PaginatedList<BlogGroup> blogGroups = await PaginatedList<BlogGroup>
                    .CreateAsync(IQ.AsNoTracking(), pageIndex ?? 1, pageSize);

                ViewBag.BlogGroupCount = _context.BlogGroups.Count();
                return View(blogGroups);
            }
            return NotFound();
        }

        [HttpPost("bloggroup")]
        public async Task<IActionResult> BlogGroup(int? pageIndex, string service, string name)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (pageIndex == null)
            {
                pageIndex = 1;
            }
            if (service == null)
            {
                return View();
            }
            if (service.Equals("searchGroup"))
            {
                if (_context.BlogGroups != null)
                {
                    IQueryable<BlogGroup> IQ = _context.BlogGroups.Where(x => x.BloggroupName.Contains(name));
                    int pageSize = 10;

                    var blogGroups  = await PaginatedList<BlogGroup>
                        .CreateAsync(IQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                    ViewBag.BlogGroupCount = _context.BlogGroups.Count();
                    return View(blogGroups);
                }
            }
            return NotFound();
        }

        [HttpGet("bloggroup/create")]
        public IActionResult CreateBlogGroup()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost("bloggroup/create")]
        public async Task<IActionResult> CreateBlogGroup(string name)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.BlogGroups != null)
            {
                BlogGroup blog = new BlogGroup()
                {
                    BloggroupId = 0,
                    BloggroupName = name
                };

                await _context.BlogGroups.AddAsync(blog);
                await _context.SaveChangesAsync();
                return RedirectToAction("BlogGroup");
            }
            return View();
        }

        [HttpGet("bloggroup/edit")]
        public  async Task<IActionResult> EditBlogGroup(int ?id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (id == null || _context.BlogGroups == null)
            {
                return RedirectToAction("BlogGroup");
            }

            var bloggroup = await _context.BlogGroups.FirstOrDefaultAsync(b => b.BloggroupId == id);

            if (bloggroup == null)
            {
                return NotFound();
            }
            return View(bloggroup);
        }

        [HttpPost("bloggroup/edit")]
        public async Task<IActionResult> EditBlogGroup(int id, string name)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.BlogGroups != null)
            {   
                
                BlogGroup blog = _context.BlogGroups.FirstOrDefault(x => x.BloggroupId == id);

                blog.BloggroupName = name;

                _context.BlogGroups.Update(blog);
                await _context.SaveChangesAsync();
                return RedirectToAction("BlogGroup");
            }
            return View();
        }


        [HttpGet("contact")]
        public async Task<IActionResult> Contact(int? pageIndex, string? service, int? id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.Contacts != null)
            {
                IQueryable<Contact> IQ = _context.Contacts.Where(x => x.Status.Equals("Active"));

                if (!string.IsNullOrEmpty(service) && service.Equals("updateStatus"))
                {
                    Contact contact = _context.Contacts.FirstOrDefault(x => x.ContactId == id);
                    contact.Status = "Deactive";
                    _context.Contacts.Update(contact);
                    await _context.SaveChangesAsync();
                    ViewBag.Hide = "Ẩn contact thành công!";
                }

                int pageSize = 10;
                var contacts = await PaginatedList<Contact>.CreateAsync(
                IQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                ViewBag.ContactListCount = _context.Contacts.Count();
                return View(contacts);
            }
            return View();
        }

        [HttpPost("contact")]
        public async Task<IActionResult> Contact(int? pageIndex, string service, string content)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (pageIndex == null)
            {
                pageIndex = 1;
            }

            if (service == null)
            {
                return View();
            }
            IQueryable<Contact> IQ = null;
            if (_context.Contacts != null)
            {
                if (string.IsNullOrEmpty(service))
                {
                     IQ = _context.Contacts.Where(x => x.Status.Equals("Active"));
                }
                if (service.Equals("searchContact"))
                {
                     IQ = _context.Contacts.Where(x => x.Content.Contains(content) && x.Status.Equals("Active"));
                }
                int pageSize = 10;
                var contacts = await PaginatedList<Contact>.CreateAsync(
                IQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                ViewBag.ContactListCount = _context.Contacts.Count();
                return View(contacts);
            }
            return View();
        }

        [HttpGet("review")]
        public async Task<IActionResult> Review(int? pageIndex, string ?service , int ?id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (_context.Reviews != null)
            {
                IQueryable<Review> IQ = _context.Reviews.Include(x => x.Restaurant).Include(x => x.Customer).Where(x => x.Status.Equals("Active"));
                if (!string.IsNullOrEmpty(service) && service.Equals("updateStatus"))
                {
                    Review review = _context.Reviews.FirstOrDefault(x => x.RateId == id);
                    review.Status = "Deactive";
                    _context.Reviews.Update(review);
                    await _context.SaveChangesAsync();
                    ViewBag.Hide = "Ẩn thành công!";
                }
                int pageSize = 10;
                var reviews = await PaginatedList<Review>.CreateAsync(
                IQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                ViewBag.ReviewsCount = _context.Reviews.Count();
                return View(reviews);
            }
            return View();
        }

        [HttpPost("review")]
        public async Task<IActionResult> Review(int? pageIndex, string ?service, string ?content)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (pageIndex == null)
            {
                pageIndex = 1;
            }

            if (service == null)
            {
                return View();
            }
            IQueryable<Review> IQ = null;
            if (_context.Reviews != null)
            {
                if (string.IsNullOrEmpty(service))
                {
                    IQ = _context.Reviews.Include(x => x.Restaurant).Include(x => x.Customer).Where(x => x.Status.Equals("Active"));
                }
                if (service.Equals("searchReview"))
                {
                    IQ = _context.Reviews.Include(x => x.Restaurant).Include(x => x.Customer).Where(x => x.Content.Contains(content) && x.Status.Equals("Active"));
                }
                int pageSize = 10;
                var reviews = await PaginatedList<Review>.CreateAsync(
                IQ.AsNoTracking(), pageIndex ?? 1, pageSize);
                ViewBag.ReviewsCount = _context.Reviews.Count();
                return View(reviews);
            }
            return View();
        }

        [HttpGet("order")]
        public async Task<IActionResult> Order(string service, string status, int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }

            ViewBag.Wait = _context.Orders.Where(x => x.Statusorder.Equals("Wait")).Count();
            ViewBag.Accept = _context.Orders.Where(x => x.Statusorder.Equals("Accept")).Count();
            ViewBag.Process = _context.Orders.Where(x => x.Statusorder.Equals("Process")).Count();
            ViewBag.Done = _context.Orders.Where(x => x.Statusorder.Equals("Done")).Count();
            ViewBag.Cancel = _context.Orders.Where(x => x.Statusorder.Equals("Cancel")).Count();
            ViewBag.Status = status;

            IQueryable<Order> ordersIQ = null;

            if (string.IsNullOrWhiteSpace(service))
            {
                service = "DisplayOrder";
            }

            if (service.Equals("DisplayOrder"))
            {
                if (_context.Orders != null)
                {
                    ordersIQ = _context.Orders
                    .Include(x => x.Customer)
                    .Include(x => x.Restaurant)
                    .OrderByDescending(x => x.OrderId);
                }
            }

            if (service == "displayOrderStatus")
            {
                if (_context.Orders != null)
                {
                    ordersIQ = _context.Orders
                   .Include(o => o.Customer)
                   .Where(x => x.Statusorder.Equals(status))
                   .OrderByDescending(x => x.OrderId);
                }
            }
            int pageSize = 10;
            List<Models.Order> orders =
                await PaginatedList<Models.Order>.CreateAsync(ordersIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
            ViewBag.OrderCount = _context.Orders.Count();
            return View(orders);
        }

        [HttpPost("order")]
        public async Task<IActionResult> Order(string status, string service, int? pageIndex, DateTime? month, DateTime? date)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            ViewBag.Wait = _context.Orders.Where(x => x.Statusorder.Equals("Wait")).Count();
            ViewBag.Accept = _context.Orders.Where(x => x.Statusorder.Equals("Accept")).Count();
            ViewBag.Process = _context.Orders.Where(x => x.Statusorder.Equals("Process")).Count();
            ViewBag.Done = _context.Orders.Where(x => x.Statusorder.Equals("Done")).Count();
            ViewBag.Cancel = _context.Orders.Where(x => x.Statusorder.Equals("Cancel")).Count();
            ViewBag.Status = status;

            IQueryable<Order> ordersIQ = null;

            if (_context.Orders != null)
            {
                if (string.IsNullOrEmpty(service))
                {
                    ordersIQ = _context.Orders.Include(o => o.Customer).Include(x => x.Restaurant);
                }

                if (service.Equals("SearchByMonth"))
                {
                    if (month != null)
                    {
                        int selectedMonth = month.Value.Month;
                        int selectedYear = month.Value.Year;
                        ordersIQ = _context.Orders.Include(o => o.Customer).Include(x => x.Restaurant)
                            .Where(x => x.CreateDate.Value.Month == selectedMonth
                            && x.CreateDate.Value.Year == selectedYear);
                    }
                    else
                    {
                        ordersIQ = _context.Orders.Include(o => o.Customer).Include(x => x.Restaurant);
                    }
                }
                if (service.Equals("SearchByDate"))
                {
                    if (date != null)
                    {
                        int selectedDay = date.Value.Day;
                        int selectedMonth = date.Value.Month;
                        int selectedYear = date.Value.Year;
                        ordersIQ = _context.Orders.Include(o => o.Customer).Include(x => x.Restaurant).
                            Where(x => x.CreateDate.Value.Day == selectedDay
                            && x.CreateDate.Value.Month == selectedMonth
                            && x.CreateDate.Value.Year == selectedYear);
                    }
                    else
                    {
                        ordersIQ = _context.Orders.Include(o => o.Customer).Include(x => x.Restaurant);
                    }
                }
            }

            int pageSize = 10;
            List<Models.Order> orders =
                await PaginatedList<Models.Order>.CreateAsync(ordersIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
            ViewBag.OrderCount = _context.Orders.Count();
            return View(orders);
        }

        [HttpGet("order/detail")]
        public IActionResult OrderDetail(int id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (id == 0)
            {
                return RedirectToAction("Order");
            }
            if(_context.Orders != null)
            {
                Models.Order order = _context.Orders.Include(o => o.Customer)
                    .Include(x => x.Restaurant).FirstOrDefault(x => x.OrderId == id);
                if (order != null)
                {
                    return View(order);
                }
            }
            return BadRequest();
        }

        [HttpGet("account/restaurant/edit")]
        public IActionResult EditRestaurant(int id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);
            return View(restaurant);
        }

        [HttpPost("account/restaurant/edit")]
        public IActionResult EditRestaurant(int id, string username, string password)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);
            restaurant.Username = username;
            restaurant.Password = password;
            _context.Restaurants.Update(restaurant);
            _context.SaveChanges();
            return RedirectToAction("Restaurant");
        }
    }
}
