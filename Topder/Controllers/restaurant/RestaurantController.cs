using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PROJECT_PRN221.Utils;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Topder.Dto;
using Topder.Models;
using Topder.Utils;
using static PROJECT_PRN221.Utils.Mail;
using static System.Net.WebRequestMethods;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Macs;

namespace Topder.Controllers.restaurant
{
    [Route("restaurant")]
    public class RestaurantController : Controller
    {
        private readonly TopderContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly MailSettings mailSettings;

        public RestaurantController(TopderContext context, IWebHostEnvironment environment)
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
                string isCustomerAuthenticated = httpContext.Session.GetString("restaurant");
                if (string.IsNullOrEmpty(isCustomerAuthenticated))
                {
                    checkS = false;
                }
            }
            return checkS;
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            var session = HttpContext.Session;

            if (session.Keys.Contains("restaurant"))
            {
                session.Remove("restaurant");
            }
            return RedirectToAction("Login");
        }

        [HttpGet("dashboard")]
        public IActionResult Dashboard(string service, int year)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }

            int idRes = 0;
            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                idRes = restaurantSession.ResId;
            }

            // HEADER
            ViewBag.TotalOrder = _context.Orders.Where(x => x.RestaurantId == idRes).Count();

            var reviews = _context.Reviews.Where(x => x.RestaurantId == idRes);
            ViewBag.Star = reviews.Any() ? (int)Math.Ceiling((decimal)reviews.Average(x => x.Star)) : 0;

            // ORDER STATUS
            ViewBag.Wait = _context.Orders.Where(x => x.Statusorder.Equals("Wait") && x.RestaurantId == idRes).Count();
            ViewBag.Accept = _context.Orders.Where(x => x.Statusorder.Equals("Accept") && x.RestaurantId == idRes).Count();
            ViewBag.Process = _context.Orders.Where(x => x.Statusorder.Equals("Process") && x.RestaurantId == idRes).Count();
            ViewBag.Success = _context.Orders.Where(x => x.Statusorder.Equals("Done") && x.RestaurantId == idRes).Count();
            ViewBag.Cancel = _context.Orders.Where(x => x.Statusorder.Equals("Cancel") && x.RestaurantId == idRes).Count();

            // Top Loyal Customers
            List<LoyalCustomer> loyalCustomer = _context.Orders
                .Where(o => o.RestaurantId == idRes)
                .Join(_context.Customers, o => o.CustomerId, c => c.CustomerId, (o, c) => new { Order = o, Customer = c })
                .GroupBy(x => new { x.Customer.CustomerId, x.Customer.Name, x.Customer.Image })
                .Select(group => new LoyalCustomer
                {
                    CustomerId = group.Key.CustomerId,
                    TotalOrder = group.Count(), // Count the total orders
                    Name = group.Key.Name,
                    Image = group.Key.Image
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
                .Where(x => x.CreateDate.Value.Year == DateTime.Now.Year && x.RestaurantId == idRes)
                .Count();
            }
            if (service.Equals("searchOrderYear"))
            {
                ViewBag.Year = year;
                yearinchart = year;
                ViewBag.TotalOrderYear = _context.Orders
                .Where(x => x.CreateDate.Value.Year == year && x.RestaurantId == idRes)
                .Count();
            }

            // CHART TOTAL INCOME YEAR
            ViewBag.t1 = getChartTotalOrderForYear(1, yearinchart, idRes);
            ViewBag.t2 = getChartTotalOrderForYear(2, yearinchart, idRes);
            ViewBag.t3 = getChartTotalOrderForYear(3, yearinchart, idRes);
            ViewBag.t4 = getChartTotalOrderForYear(4, yearinchart, idRes);
            ViewBag.t5 = getChartTotalOrderForYear(5, yearinchart, idRes);
            ViewBag.t6 = getChartTotalOrderForYear(6, yearinchart, idRes);
            ViewBag.t7 = getChartTotalOrderForYear(7, yearinchart, idRes);
            ViewBag.t8 = getChartTotalOrderForYear(8, yearinchart, idRes);
            ViewBag.t9 = getChartTotalOrderForYear(9, yearinchart, idRes);
            ViewBag.t10 = getChartTotalOrderForYear(10, yearinchart, idRes);
            ViewBag.t11 = getChartTotalOrderForYear(11, yearinchart, idRes);
            ViewBag.t12 = getChartTotalOrderForYear(12, yearinchart, idRes);


            // Chart Age 
            var currentDate = DateTime.Now;
            var customersByAgeGroups = from order in _context.Orders
                                       join customer in _context.Customers on order.CustomerId equals customer.CustomerId
                                       join restaurant in _context.Restaurants on order.RestaurantId equals restaurant.ResId
                                       where order.RestaurantId == idRes && customer.Dob.HasValue
                                       let age = currentDate.Year - customer.Dob.Value.Year -
                                                 ((currentDate.Month < customer.Dob.Value.Month ||
                                                   (currentDate.Month == customer.Dob.Value.Month && currentDate.Day < customer.Dob.Value.Day)) ? 1 : 0)
                                       group customer by age into ageGroup
                                       select new
                                       {
                                           Age = ageGroup.Key,
                                           CustomerCount = ageGroup.Select(c => c.CustomerId).Distinct().Count()
                                       };


            ViewBag.Under18Age = customersByAgeGroups.Where(a => a.Age < 18).Sum(a => a.CustomerCount);
            ViewBag.Between18And24Age = customersByAgeGroups.Where(a => a.Age >= 18 && a.Age <= 24).Sum(a => a.CustomerCount);
            ViewBag.Between25And34Age = customersByAgeGroups.Where(a => a.Age >= 25 && a.Age <= 34).Sum(a => a.CustomerCount);
            ViewBag.Between35And44Age = customersByAgeGroups.Where(a => a.Age >= 35 && a.Age <= 44).Sum(a => a.CustomerCount);
            ViewBag.Between45And54Age = customersByAgeGroups.Where(a => a.Age >= 45 && a.Age <= 54).Sum(a => a.CustomerCount);
            ViewBag.Above55Age = customersByAgeGroups.Where(a => a.Age >= 55).Sum(a => a.CustomerCount);


            // Chart Star 
            var reviewCounts = from review in _context.Reviews
                               where review.RestaurantId == idRes
                               group review by review.Star into starGroup
                               select new
                               {
                                   Star = starGroup.Key,
                                   Count = starGroup.Count()
                               };
            ViewBag.Star1 = reviewCounts.FirstOrDefault(r => r.Star == 1)?.Count ?? 0;
            ViewBag.Star2 = reviewCounts.FirstOrDefault(r => r.Star == 2)?.Count ?? 0;
            ViewBag.Star3 = reviewCounts.FirstOrDefault(r => r.Star == 3)?.Count ?? 0;
            ViewBag.Star4 = reviewCounts.FirstOrDefault(r => r.Star == 4)?.Count ?? 0;
            ViewBag.Star5 = reviewCounts.FirstOrDefault(r => r.Star == 5)?.Count ?? 0;


            //Console.WriteLine(ViewBag.Star1);
            //Console.WriteLine(ViewBag.Under18Age);
            //Console.WriteLine(ViewBag.t1);

            return View(loyalCustomer);
        }

        private int getChartTotalOrderForYear(int mounth, int year, int idRes)
        {
            int totalOrder = _context.Orders
            .Where(o => o.CreateDate.Value.Month == mounth && o.CreateDate.Value.Year == year && o.RestaurantId == idRes)
            .Count();
                return totalOrder;
        }

        [HttpGet("authenticate/register")]
        public IActionResult Register()
        {

            List<string> timeList = new List<string>();
            for (int hour = 0; hour < 24; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    timeList.Add($"{hour:D2}:{minute:D2}");
                }
            }

            ViewBag.Category = _context.Categories.ToList();
            ViewBag.TimeList = timeList;
            return View();
        }

        [HttpPost("authenticate/register")]
        public async Task<IActionResult> Register(string nameres, string nameowner, IFormFile inputFile, string address,
            string location, TimeSpan opentime, TimeSpan closetime,string email,string phone, int category_id
            , string username, string pass, string repass)
        {
            bool checkInput = true;

            if (!Utils.Validation.IsUsernameRestaurant(username, _context))
            {
                ViewBag.UsernameError = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";
                checkInput = false;
            }
            if (opentime >= closetime)
            {
                ViewBag.TimeError = "Thời gian mở cửa phải nhỏ hơn thời gian đóng cửa";
                checkInput = false;
            }

            if (!Utils.Validation.IsEmailValid(email))
            {
                ViewBag.EmailError = "Định dạng email không hợp lệ.";
                checkInput = false;
            }

            if (!Utils.Validation.IsPasswordValid(pass))
            {
                ViewBag.PasswordError = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ thường, chữ hoa và một số.";
                checkInput = false;
            }

            if (pass != repass)
            {
                ViewBag.RepasswordError = "Mật khẩu và nhập lại mật khẩu không khớp.";
                checkInput = false;
            }

            if (checkInput == true)
            {   
                Restaurant restaurant = new Restaurant()
                {
                    NameOwner = nameowner,
                    NameRes = nameres,
                    Username = username,
                    Password = pass,
                    Email = email,
                    OpenTime = opentime,
                    CloseTime = closetime,
                    Address = address,
                    Location = location,
                    Phone = phone,
                    CategoryId = category_id,
                    Status = "Deactive"
                };


                //// Upload file 
                string fileURL = string.Empty;
                if (inputFile != null && inputFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images/restaurant");
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(inputFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await inputFile.CopyToAsync(fileStream);
                        fileURL = "/Images/restaurant/" + uniqueFileName;
                    }
                    restaurant.Image = fileURL;
                }

                _context.Restaurants.Add(restaurant);
                
                if(_context.SaveChanges() > 0)
                {
                    //SEND OTP
                    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    var logger = loggerFactory.CreateLogger<SendMailService>();
                    Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                    sendMailService.SendEmailAsync(restaurant.Email, "Register", $"Xin Chào {restaurant.NameRes},\n\nBạn đã đăng ký thành công," +
                        " Hãy đợi chúng tôi kiểm duyệt và nhà hàng có mặt trên Topder sớm nhất có thể.\n\nCảm ơn vì sự có mặt của bạn!");
                    sendMailService.SendEmailAsync("topder.vn@gmail.com", "Register", $"Nhà hàng {restaurant.NameRes},\n\n Đã đăng ký dùng dịch vụ của Topder ," +
                        " Hãy xem qua nhà hàng và active.\n\nCảm ơn!");
                    return RedirectToAction("Home", "Customer");
                }
            }
            return RedirectToAction("Register");
        }


        [HttpGet("authenticate/login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("authenticate/login")]
        public IActionResult Login(string username, string password)
        {
            if (_context != null)
            {
                if (username == string.Empty || password == string.Empty)
                {
                    ViewBag.ErrorMess = "Tên đăng nhập hoặc mật khẩu không được bỏ trống!";
                    return View();
                }
                else
                {
                    Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.Username.Equals(username) && x.Password.Equals(password));

                    if (restaurant != null)
                    {
                        if (restaurant.Status.Equals("Deactive"))
                        {
                            ViewBag.ErrorMess = "Tài khoản đang bị vô hiệu hóa.";
                            return View();
                        }
                        string restaurantSession = JsonConvert.SerializeObject(restaurant);
                        HttpContext.Session.SetString("restaurant", restaurantSession);
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        ViewBag.ErrorMess = "Tên đăng nhập hoặc mật khẩu sai! vui lòng thử lại";
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
            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.Username.Equals(username) && x.Email.Equals(email));

            if (restaurant != null)
            {
                string otp = Utils.Validation.GenerateOTP(6);

                // SET SESSION
                HttpContext.Session.SetString("otp", otp);
                HttpContext.Session.SetString("username", username);

                //SEND OTP
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger<SendMailService>();
                Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                sendMailService.SendEmailAsync(email, "OTP", otp);
                return RedirectToAction("OTP");
            }
            else
            {
                ViewBag.ErrorMess = "Tài khoản hoặc email sai! vui lòng nhập lại.";
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
                Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.Username.Equals(username));
                restaurant.Password = pass;
                _context.Restaurants.Update(restaurant);
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
            return View();
        }

        [HttpPost("changespassword")]
        public IActionResult ChangesPassword(string newpass, string renewpass)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            bool checkInput = true;
            if (!Utils.Validation.IsPasswordValid(newpass))
            {
                ViewBag.PasswordError = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ thường, chữ hoa và một số.";
                checkInput = false;
            }

            if (!newpass.Equals(renewpass))
            {
                ViewBag.RepasswordError = "Mật khẩu và nhập lại mật khẩu không khớp.";
                checkInput = false;
            }

            if (checkInput == true)
            {
                string restaurantJson = HttpContext.Session.GetString("restaurant");
                int id = 0;
                if (!string.IsNullOrEmpty(restaurantJson))
                {
                    Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                    id = restaurantSession.ResId;
                }
                Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);
                restaurant.Password = newpass;
                _context.Restaurants.Update(restaurant);
                _context.SaveChanges();
                ViewBag.ChangesPasswordSuccess = "Đổi mật khẩu thành công!";
                return View();
            }
            return View();
        }


        // Information VIEW
        [HttpGet("information")]
        public IActionResult Information()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                id = restaurantSession.ResId;
            }

            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);
            return View(restaurant);
        }

        [HttpGet("information/edit")]
        public IActionResult EditInformation(int id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            List<string> timeList = new List<string>();
            for (int hour = 0; hour < 24; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    timeList.Add($"{hour:D2}:{minute:D2}");
                }
            }
            ViewBag.TimeList = timeList;
            ViewBag.Category = _context.Categories.ToList();
            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);
            return View(restaurant);
        }

        [HttpPost("information/edit")]
        public async Task<IActionResult> EditInformation(int id,string resname, string ownername, string address, string phone, string email,
            TimeSpan opentime, TimeSpan closetime, string location, IFormFile fileUpload)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            bool checkInput = true;
            if (opentime >= closetime)
            {
                ViewBag.TimeError = "Thời gian mở cửa phải nhỏ hơn thời gian đóng cửa";
                checkInput = false;
            }

            if (!Utils.Validation.IsEmailValid(email))
            {
                ViewBag.EmailError = "Định dạng email không hợp lệ.";
                checkInput = false;
            }

            if (checkInput == true)
            {
                Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);

                //Update restaurant
                if(restaurant != null)
                {
                    restaurant.NameRes = resname;
                    restaurant.NameOwner = ownername;
                    restaurant.Address = address;
                    restaurant.Phone = phone;
                    restaurant.Email = email;
                    restaurant.OpenTime = opentime;
                    restaurant.CloseTime = closetime;
                    restaurant.Location = location;

                    //// Upload file 
                    string fileURL = string.Empty;
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images/restaurant");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(fileStream);
                            fileURL = "/Images/restaurant/" + uniqueFileName;
                        }
                        restaurant.Image = fileURL;
                    }

                    _context.Restaurants.Update(restaurant);
                    if (_context.SaveChanges() > 0)
                    {
                        return RedirectToAction("Information");
                    }
                }
            }
            return View();
        }

        [HttpGet("image")]
        public async Task<IActionResult> Image(int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                id = restaurantSession.ResId;
            }

            IQueryable<Models.Image> imageIQ = _context.Images
                .Include(x => x.Restaurant)
                .Where(x => x.RestaurantId == id);

            int pageSize = 10;
            List<Models.Image> images =
                await PaginatedList<Models.Image>.CreateAsync(imageIQ.AsNoTracking(), pageIndex ?? 1, pageSize);

            return View(images);
        }


        [HttpGet("image/create")]
        public IActionResult CreateImage()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost("image/create")]
        public async Task<IActionResult> CreateImage(IFormFile fileUpload)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                id = restaurantSession.ResId;
            }

            //// Upload file 
            string fileURL = string.Empty;
            if (fileUpload != null && fileUpload.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images/resimg");
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileUpload.CopyToAsync(fileStream);
                    fileURL = "/Images/resimg/" + uniqueFileName;
                }
                Image image = new Image()
                {
                    ImageId = 0,
                    ImageUrl = fileURL,
                    RestaurantId = id
                };
                _context.Images.Add(image);
                if(_context.SaveChanges() > 0)
                {
                    return RedirectToAction("Image");
                }
            }
            return View();
        }

        [HttpGet("image/edit")]
        public IActionResult EditImage(int id)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            if (id == 0)
            {
                RedirectToAction("Image");
            }
            Image image = _context.Images.FirstOrDefault(x => x.ImageId == id);
            return View(image);
        }

        [HttpPost("image/edit")]
        public async Task<IActionResult> EditImage(int id, IFormFile fileUpload)
        {   
            Image image = _context.Images.FirstOrDefault(y => y.ImageId == id);

            //// Upload file 
            string fileURL = string.Empty;
            if (fileUpload != null && fileUpload.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images/resimg");
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileUpload.CopyToAsync(fileStream);
                    fileURL = "/Images/resimg/" + uniqueFileName;
                }
                image.ImageUrl = fileURL;
                _context.Images.Update(image);
                if (_context.SaveChanges() > 0)
                {
                    return RedirectToAction("Image");
                }
            }
            return RedirectToAction("Image");
        }

        [HttpGet("description")]
        public IActionResult Description()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;

            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                id = restaurantSession.ResId;
            }

            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);

            return View(restaurant);
        }

        [HttpGet("description/edit")]
        public IActionResult EditDescription()
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;

            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                id = restaurantSession.ResId;
            }

            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);

            return View(restaurant);
        }

        [HttpPost("description/edit")]
        public IActionResult EditDescription(int id ,string subdescription, string description)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == id);

            restaurant.Subdescription = subdescription;
            restaurant.Description = description;
            _context.Restaurants.Update(restaurant);
            if(_context.SaveChanges() > 0)
            {
                return RedirectToAction("Description");
            }
            return View();
        }


        [HttpGet("order")]
        public async Task<IActionResult> Order(string service, string status, int? pageIndex, DateTime? month, DateTime? date)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                id = restaurantSession.ResId;
            }

            ViewBag.Wait = _context.Orders.Where(x => x.Statusorder.Equals("Wait") && x.RestaurantId == id).Count();
            ViewBag.Accept = _context.Orders.Where(x => x.Statusorder.Equals("Accept") && x.RestaurantId == id).Count();
            ViewBag.Process = _context.Orders.Where(x => x.Statusorder.Equals("Process") && x.RestaurantId == id).Count();
            ViewBag.Done = _context.Orders.Where(x => x.Statusorder.Equals("Done") && x.RestaurantId == id).Count();
            ViewBag.Cancel = _context.Orders.Where(x => x.Statusorder.Equals("Cancel") && x.RestaurantId == id).Count();
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
                        .Where(x => x.RestaurantId == id)
                        .OrderByDescending(x => x.OrderId);

                }
            }

            if (service == "displayOrderStatus")
            {
                if (_context.Orders != null)
                {
                         ordersIQ = _context.Orders
                        .Include(o => o.Customer)
                        .Where(x => x.Statusorder.Equals(status) && x.RestaurantId == id)
                        .OrderByDescending(x => x.OrderId);
                }
            }

            if (service == "SearchByMonth")
            {
                if (_context.Orders != null)
                {
                    ordersIQ = _context.Orders
                   .Include(o => o.Customer)
                   .Where(x => x.Statusorder.Equals(status) && x.RestaurantId == id)
                   .OrderByDescending(x => x.OrderId);
                }
            }


            if (service.Equals("SearchByMonth"))
            {
                if (month != null)
                {
                    int selectedMonth = month.Value.Month;
                    int selectedYear = month.Value.Year;
                    ordersIQ = _context.Orders.Include(o => o.Customer)
                        .Where(x => x.CreateDate.Value.Month == selectedMonth
                        && x.CreateDate.Value.Year == selectedYear && x.RestaurantId == id);
                }
                else
                {
                    ordersIQ = _context.Orders.Include(o => o.Customer).Where(x => x.RestaurantId == id);
                }
            }
            if (service.Equals("SearchByDate"))
            {
                if (date != null)
                {
                    int selectedDay = date.Value.Day;
                    int selectedMonth = date.Value.Month;
                    int selectedYear = date.Value.Year;
                    ordersIQ = _context.Orders.Include(o => o.Customer).
                        Where(x => x.CreateDate.Value.Day == selectedDay
                        && x.CreateDate.Value.Month == selectedMonth
                        && x.CreateDate.Value.Year == selectedYear && x.RestaurantId == id);
                }
                else
                {
                    ordersIQ = _context.Orders.Include(o => o.Customer).Where(x => x.RestaurantId == id);
                }
            }

            int pageSize = 10;
            List<Models.Order> orders =
                await PaginatedList<Models.Order>.CreateAsync(ordersIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
            ViewBag.OrderCount = _context.Orders.Where(x => x.RestaurantId == id).Count();
            ViewBag.date = date;
            ViewBag.month = month;
            ViewBag.service = service;
            ViewBag.status = status;
            return View(orders);
        }

        [HttpPost("order")]
        public async Task<IActionResult> Order(int id, string status, string service, int? pageIndex, DateTime? month, DateTime? date)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int idRes = 0;
            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                idRes = restaurantSession.ResId;
            }

            ViewBag.Wait = _context.Orders.Where(x => x.Statusorder.Equals("Wait") && x.RestaurantId == idRes).Count();
            ViewBag.Accept = _context.Orders.Where(x => x.Statusorder.Equals("Accept") && x.RestaurantId == idRes).Count();
            ViewBag.Process = _context.Orders.Where(x => x.Statusorder.Equals("Process") && x.RestaurantId == idRes).Count();
            ViewBag.Done = _context.Orders.Where(x => x.Statusorder.Equals("Done") && x.RestaurantId == idRes).Count();
            ViewBag.Cancel = _context.Orders.Where(x => x.Statusorder.Equals("Cancel") && x.RestaurantId == idRes).Count();
            ViewBag.Status = status;

            IQueryable<Order> ordersIQ = null;
            if (pageIndex == null)
            {
                pageIndex = 1;
            }
            if (_context.Orders != null)
            {
                if (service.Equals("updateStatus"))
                {
                    Order order = _context.Orders.Include(x => x.Customer).Include(x => x.Restaurant).FirstOrDefault(x => x.OrderId == id);
                    if (!order.Statusorder.Equals(status))
                    {
                        order.Statusorder = status;

                        if (status.Equals("Accept"))
                        {
                            order.AccpectDate = DateTime.Now;
                        }
                        if (status.Equals("Process"))
                        {
                            order.ProcessDate = DateTime.Now;
                        }
                        if (status.Equals("Done"))
                        {
                            order.DoneDate = DateTime.Now;
                        }
                        if (status.Equals("Cancel"))
                        {
                            order.CancelDate = DateTime.Now;
                        }

                        _context.Orders.Update(order);
                        if (_context.SaveChanges() > 0)
                        {
                            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                            var logger = loggerFactory.CreateLogger<SendMailService>();
                            Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                            sendMailService.SendEmailAsync(order.Customer.Email, "UpdateOrderStatus", order.OrderId.ToString());
                        }
                    }
                    ordersIQ = _context.Orders.Include(o => o.Customer).Where(x => x.Statusorder.Equals(status) && x.RestaurantId == idRes);
                }
                if (service.Equals("SearchByMonth"))
                {
                    if (month != null)
                    {
                        int selectedMonth = month.Value.Month;
                        int selectedYear = month.Value.Year;
                        ordersIQ = _context.Orders.Include(o => o.Customer)
                            .Where(x => x.CreateDate.Value.Month == selectedMonth
                            && x.CreateDate.Value.Year == selectedYear && x.RestaurantId == idRes);
                    }
                    else
                    {
                        ordersIQ = _context.Orders.Include(o => o.Customer).Where(x => x.RestaurantId == idRes);
                    }
                }
                if (service.Equals("SearchByDate"))
                {
                    if (date != null)
                    {
                        int selectedDay = date.Value.Day;
                        int selectedMonth = date.Value.Month;
                        int selectedYear = date.Value.Year;
                        ordersIQ = _context.Orders.Include(o => o.Customer).
                            Where(x => x.CreateDate.Value.Day == selectedDay
                            && x.CreateDate.Value.Month == selectedMonth
                            && x.CreateDate.Value.Year == selectedYear && x.RestaurantId == idRes);
                    }
                    else
                    {
                        ordersIQ = _context.Orders.Include(o => o.Customer).Where(x => x.RestaurantId == idRes);
                    }
                }
            }

            int pageSize = 10;
            List<Models.Order> orders =
                await PaginatedList<Models.Order>.CreateAsync(ordersIQ.AsNoTracking(), pageIndex ?? 1, pageSize);
            ViewBag.OrderCount = _context.Orders.Where(x => x.RestaurantId == idRes).Count();
            ViewBag.date = date;
            ViewBag.month = month;
            ViewBag.service = service;
            ViewBag.status = status;
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

            int idRes = 0;
            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                idRes = restaurantSession.ResId;
            }

            Models.Order order = _context.Orders.FirstOrDefault(x => x.OrderId == id && x.RestaurantId == idRes);

            return View(order);
        }

        [HttpGet("review")]
        public async Task<IActionResult> Review(int? pageIndex)
        {
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string restaurantJson = HttpContext.Session.GetString("restaurant");

            if (!string.IsNullOrEmpty(restaurantJson))
            {
                Restaurant restaurantSession = JsonConvert.DeserializeObject<Restaurant>(restaurantJson);
                id = restaurantSession.ResId;
            }

            IQueryable<Models.Review> reviewsIQ = _context.Reviews
                .Include(x => x.Customer)
                .Include(x => x.Restaurant)
                .Where(x => x.RestaurantId == id && x.Status.Equals("Active"))
                .OrderByDescending(x => x.RateId);

            int pageSize = 10;
            List<Models.Review> reviews =
                await PaginatedList<Models.Review>.CreateAsync(reviewsIQ.AsNoTracking(), pageIndex ?? 1, pageSize);

            return View(reviews);
        }

    }
}
