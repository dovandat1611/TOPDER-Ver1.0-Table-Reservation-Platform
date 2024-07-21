using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PROJECT_PRN221.Utils;
using System;
using Topder.Models;
using Topder.Utils;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using static PROJECT_PRN221.Utils.Mail;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Topder.Dto;
using static System.Net.WebRequestMethods;

namespace Topder.Controllers.customer
{
    [Route("customer")]
    public class CustomerController : Controller
    {

        private readonly TopderContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly MailSettings mailSettings;

        public CustomerController(TopderContext context, IWebHostEnvironment environment)
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
                string isCustomerAuthenticated = httpContext.Session.GetString("customer");
                if (string.IsNullOrEmpty(isCustomerAuthenticated))
                {
                    checkS = false;
                }
            }

            return checkS;
        }

        [HttpGet("home")]
        public IActionResult Home()
        {
            ViewBag.IsLogin = checkSession();
            if (_context.Restaurants != null)
            {
                List<Restaurant> Newrestaurants = _context.Restaurants.Include(x => x.Category)
                    .Where(x => x.Status.Equals("Active"))
                    .OrderByDescending(x => x.ResId)
                    .Take(4)
                    .ToList();

                if (Newrestaurants != null && Newrestaurants.Any())
                {
                    List<RestaurantDTO> restaurantDTOs = new List<RestaurantDTO>();

                    foreach (var restaurant in Newrestaurants)
                    {
                        int totalReviews = _context.Reviews.Count(x => x.RestaurantId == restaurant.ResId);
                        var reviews = _context.Reviews.Where(x => x.RestaurantId == restaurant.ResId).ToList();
                        int star = reviews.Any() ? (int)Math.Ceiling((decimal)reviews.Average(x => x.Star)) : 0;

                        RestaurantDTO restaurantDTO = new RestaurantDTO
                        {
                            ResId = restaurant.ResId,
                            Image = restaurant.Image,
                            NameRes = restaurant.NameRes,
                            CategoryName = restaurant.Category.CategoryName,
                            TotalReviews = totalReviews,
                            Star = star
                        };

                        restaurantDTOs.Add(restaurantDTO);
                    }
                    ViewBag.NewRestaurants = restaurantDTOs;
                    ViewBag.NewRestaurantsCount = restaurantDTOs.Count();
                }


                List<Restaurant> TopOrderRestaurants = _context.Restaurants
                    .Include(x => x.Category)
                    .Where(x => x.Status == "Active")
                    .OrderByDescending(r => _context.Orders.Count(o => o.RestaurantId == r.ResId))
                    .ThenByDescending(r => r.ResId)
                    .Take(8)
                    .ToList();

                if (TopOrderRestaurants != null && TopOrderRestaurants.Any())
                {
                    List<RestaurantDTO> restaurantDTOs = new List<RestaurantDTO>();

                    foreach (var restaurant in TopOrderRestaurants)
                    {
                        int totalReviews = _context.Reviews.Count(x => x.RestaurantId == restaurant.ResId);
                        var reviews = _context.Reviews.Where(x => x.RestaurantId == restaurant.ResId).ToList();
                        int star = reviews.Any() ? (int)Math.Ceiling((decimal)reviews.Average(x => x.Star)) : 0;

                        RestaurantDTO restaurantDTO = new RestaurantDTO
                        {
                            ResId = restaurant.ResId,
                            Image = restaurant.Image,
                            NameRes = restaurant.NameRes,
                            CategoryName = restaurant.Category.CategoryName,
                            TotalReviews = totalReviews,
                            Star = star
                        };
                        restaurantDTOs.Add(restaurantDTO);
                    }
                    ViewBag.TopOrderRestaurant = restaurantDTOs;
                    ViewBag.TopOrderRestaurantCount = restaurantDTOs.Count();
                }

                List<Restaurant> reputationRestaurant = _context.Restaurants.Include(x => x.Category)
                    .Where(x => x.Status.Equals("Active"))
                    .ToList();

                if (reputationRestaurant != null && reputationRestaurant.Any())
                {
                    List<RestaurantDTO> restaurantDTOs = new List<RestaurantDTO>();

                    foreach (var restaurant in reputationRestaurant)
                    {
                        int totalReviews = _context.Reviews.Count(x => x.RestaurantId == restaurant.ResId);
                        var reviews = _context.Reviews.Where(x => x.RestaurantId == restaurant.ResId).ToList();
                        int star = reviews.Any() ? (int)Math.Ceiling((decimal)reviews.Average(x => x.Star)) : 0;

                        RestaurantDTO restaurantDTO = new RestaurantDTO
                        {
                            ResId = restaurant.ResId,
                            Image = restaurant.Image,
                            NameRes = restaurant.NameRes,
                            CategoryName = restaurant.Category.CategoryName,
                            TotalReviews = totalReviews,
                            Star = star
                        };
                        restaurantDTOs.Add(restaurantDTO);
                    }
                    List<RestaurantDTO> topStarRestaurant = restaurantDTOs.OrderByDescending(r => r.Star)
                                                          .ThenByDescending(r => r.TotalReviews)
                                                          .Take(8).ToList();

                    ViewBag.topReputationRestaurant = topStarRestaurant;
                    ViewBag.topReputationRestaurantCount = topStarRestaurant.Count();
                }
            }
            List<Blog> blogs = _context.Blogs.Include(x => x.Bloggroup).Include(x => x.Admin).Take(8).OrderByDescending(x => x.BlogId).ToList();
            ViewBag.Blog = blogs;
            ViewBag.BlogCount = blogs.Count();
            ViewBag.MessSuccess = TempData["MessSuccess"];
            ViewBag.MessError = TempData["MessError"];
            return View();
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            ViewBag.IsLogin = checkSession();
            var session = HttpContext.Session;

            if (session.Keys.Contains("customer"))
            {
                session.Remove("customer");
            }
            return RedirectToAction("Login");
        }

        [HttpGet("authenticate/login")]
        public IActionResult Login()
        {
            ViewBag.IsLogin = checkSession();
            return View();
        }


        [HttpPost("authenticate/login")]
        public IActionResult Login(string username, string password)
        {
            ViewBag.IsLogin = checkSession();
            if (_context != null)
            {
                if (username == string.Empty || password == string.Empty)
                {
                    ViewBag.Message = "Tên đăng nhập hoặc mật khẩu không được bỏ trống!";
                    return View();
                }
                else
                {
                    Customer customer = _context.Customers.FirstOrDefault(x => x.Username.Equals(username) && x.Password.Equals(password));

                    if (customer != null)
                    {
                        if (customer.Status.Equals("Deactive"))
                        {
                            ViewBag.Message = "Tài khoản đang bị vô hiệu hóa.";
                            return View();
                        }
                        string customerSession = JsonConvert.SerializeObject(customer);
                        HttpContext.Session.SetString("customer", customerSession);
                        return RedirectToAction("Home");
                    }
                    else
                    {
                        ViewBag.Message = "Tên đăng nhập hoặc mật khẩu sai! vui lòng thử lại";
                        return View();
                    }
                }
            }
            return View();
        }

        [HttpGet("authenticate/register")]
        public IActionResult Register()
        {
            ViewBag.IsLogin = checkSession();
            return View();
        }


        [HttpPost("authenticate/register")]
        public IActionResult Register(string name, string phone, string email, DateTime dob, string username, string password, string repassword, string gender)
        {
            ViewBag.IsLogin = checkSession();
            bool checkInput = true;

            if (!Utils.Validation.IsUsernameUnique(username, _context))
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

            if (password != repassword)
            {
                ViewBag.RepasswordError = "Mật khẩu và nhập lại mật khẩu không khớp.";
                checkInput = false;
            }
            if (checkInput == true)
            {

                Customer customer = new Customer() {
                    Name = name,
                    Phone = phone,
                    Email = email,
                    Gender = gender,
                    Dob = dob,
                    Username = username,
                    Password = password,
                    Status = "Active"
                };

                _context.Customers.Add(customer);
                if(_context.SaveChanges() >0)
                {
                    //SEND MAIL
                    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    var logger = loggerFactory.CreateLogger<SendMailService>();
                    Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                    sendMailService.SendEmailAsync(customer.Email, "Register", $"Xin Chào {customer.Name},\n\nBạn đã đăng ký thành công tài khoản trên Topder.\n\nCảm ơn vì sự có mặt của bạn!");
                }  
                return RedirectToAction("Login");
            }
            return View();
        }


        [HttpGet("authenticate/forgotpassword")]
        public IActionResult ForgotPassword()
        {
            ViewBag.IsLogin = checkSession();
            return View();
        }

        [HttpPost("authenticate/forgotpassword")]
        public IActionResult ForgotPassword(string username, string email)
        {
            ViewBag.IsLogin = checkSession();
            Customer customer = _context.Customers.FirstOrDefault(x => x.Username.Equals(username) && x.Email.Equals(email));

            if (customer != null)
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
                ViewBag.Error = "Tài khoản hoặc email sai! vui lòng nhập lại.";
                return View();
            }
        }

        [HttpGet("authenticate/otp")]
        public IActionResult OTP()
        {
            ViewBag.IsLogin = checkSession();
            return View();
        }

        [HttpPost("authenticate/otp")]
        public IActionResult OTP(string otp)
        {
            ViewBag.IsLogin = checkSession();
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
            ViewBag.IsLogin = checkSession();
            return View();
        }

        [HttpPost("authenticate/resetpassword")]
        public IActionResult ResetPassword(string pass, string repass)
        {
            ViewBag.IsLogin = checkSession();
            string username = HttpContext.Session.GetString("username");

            if (pass.Equals(repass) && Utils.Validation.IsPasswordValid(pass))
            {
                Customer customer = _context.Customers.FirstOrDefault(x => x.Username.Equals(username));
                customer.Password = pass;
                _context.Customers.Update(customer);
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
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");

            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }
            ViewBag.Customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);
            return View();
        }


        [HttpPost("changespassword")]
        public IActionResult ChangesPassword(string newpassword, string renewpassword)
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }

            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");
            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }
            Customer customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);
            ViewBag.Customer = customer;
            bool checkInput = true;
            if (!Utils.Validation.IsPasswordValid(newpassword))
            {
                ViewBag.PasswordError = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ thường, chữ hoa và một số.";
                checkInput = false;
            }

            if (!newpassword.Equals(renewpassword))
            {
                ViewBag.RepasswordError = "Mật khẩu và nhập lại mật khẩu không khớp.";
                checkInput = false;
            }

            if (checkInput == true)
            {
                customer.Password = newpassword;
                _context.Customers.Update(customer);
                _context.SaveChanges();
                ViewBag.ChangesPasswordSuccess = "Đổi Mật Khẩu Thành Công!";
                return View();
            }

            return View();
        }

        // PROFILE VIEW
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");
            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }
            Customer customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);
            return View(customer);
        }

        [HttpGet("profile/edit")]
        public IActionResult EditProfile()
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");
            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }
            Customer customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);
            return View(customer);
        }

        [HttpPost("profile/edit")]
        public async Task<IActionResult> EditProfile(int id, string name, DateTime dob, string gender, string phone, string email, IFormFile fileUpload)
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            Customer customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            customer.Name = name;
            customer.Gender = gender;
            customer.Phone = phone;
            customer.Email = email;
            customer.Dob = dob;

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
                customer.Image = fileURL;
            }

            _context.Customers.Update(customer);
            _context.SaveChanges();
            return RedirectToAction("Profile");
        }



        // service VIEW
        [HttpGet("service")]
        public async Task<IActionResult> Service(int? pageIndex, int? categoryid, string? location, string? nameRes)
        {
            ViewBag.IsLogin = checkSession();

            if (_context.Restaurants != null)
            {
                IQueryable<Restaurant> query = _context.Restaurants.Include(x => x.Category).Where(x => x.Status.Equals("Active"));

                if (categoryid != null && categoryid > 0)
                {
                    query = query.Where(x => x.CategoryId == categoryid);
                }

                if (!string.IsNullOrEmpty(location) && location != "All")
                {
                    query = query.Where(x => x.Location.Contains(location));
                }

                if (!string.IsNullOrEmpty(nameRes))
                {
                    query = query.Where(x => x.NameRes.Contains(nameRes));
                }

                var restaurantDTOs = query.Select(x => new RestaurantDTO
                {
                    ResId = x.ResId,
                    Image = x.Image,
                    NameRes = x.NameRes,
                    CategoryName = x.Category.CategoryName,
                    TotalReviews = _context.Reviews.Count(r => r.RestaurantId == x.ResId),
                    Star = (int)Math.Ceiling(_context.Reviews.Where(r => r.RestaurantId == x.ResId).DefaultIfEmpty().Average(r => (int?)r.Star ?? 0))
                });

                int pageSize = 12;
                PaginatedList<RestaurantDTO> restaurants = await PaginatedList<RestaurantDTO>.CreateAsync(restaurantDTOs.AsNoTracking(), pageIndex ?? 1, pageSize);

                List<Category> categories = await _context.Categories.ToListAsync();
                List<string> locations = await _context.Restaurants.Select(x => x.Location).Distinct().ToListAsync();

                ViewBag.Category = categories;
                ViewBag.CategoryCount = categories.Count();
                ViewBag.LocationList = locations;
                ViewBag.LocationCount = locations.Count();
                ViewBag.nameRes = nameRes;
                ViewBag.location = location;
                ViewBag.categoryid = categoryid;
                ViewBag.MessSuccess = TempData["MessSuccess"];
                ViewBag.MessError = TempData["MessError"];
                return View(restaurants);
            }
            return NotFound();
        }

        // RESTAURANT VIEW
        [HttpGet("restaurant")]
        public async Task<IActionResult>  Restaurant(int? id, int? pageIndex, string ?service)
        {
            ViewBag.IsLogin = checkSession();
            if (id == null || id == 0)
            {
                return RedirectToAction("Home");
            }

            if (_context.Restaurants != null)
            {
                Restaurant restaurant = _context.Restaurants.Include(x => x.Category).FirstOrDefault(x => x.ResId == id);
                if(restaurant == null || restaurant.Status.Equals("Deactive"))
                {
                    return NotFound();
                }

                List<Restaurant> RelateRestaurants = _context.Restaurants
                    .Include(x => x.Category)
                    .Where(x => x.CategoryId == restaurant.CategoryId && x.ResId != restaurant.ResId)
                    .ToList();

                if (RelateRestaurants != null && RelateRestaurants.Any())
                {
                    List<RestaurantDTO> relatedRestaurantDTOs = new List<RestaurantDTO>();

                    foreach (var relatedRestaurant in RelateRestaurants)
                    {
                        var realtereviews = _context.Reviews.Where(x => x.RestaurantId == relatedRestaurant.ResId).ToList();
                        int realtetotalReviews = realtereviews.Count;
                        int relatestar = realtereviews.Any() ? (int)Math.Ceiling((decimal)realtereviews.Average(x => x.Star)) : 0;

                        RestaurantDTO restaurantDTO = new RestaurantDTO
                        {
                            ResId = relatedRestaurant.ResId,
                            Image = relatedRestaurant.Image,
                            NameRes = relatedRestaurant.NameRes,
                            CategoryName = relatedRestaurant.Category.CategoryName,
                            TotalReviews = realtetotalReviews,
                            Star = relatestar
                        };

                        relatedRestaurantDTOs.Add(restaurantDTO);
                    }
                    ViewBag.RelateRestaurants = relatedRestaurantDTOs;
                    ViewBag.RelateRestaurantsCount = relatedRestaurantDTOs.Count();
                }

                List<Review> reviews = null;
                IQueryable<Review> reviewsIQ = null;
                int pageSize = 3;
                if (!string.IsNullOrEmpty(service) && service.Equals("review"))
                {
                    reviewsIQ = _context.Reviews.Include(x => x.Customer).Include(x => x.Restaurant).Where(x => x.RestaurantId == id && x.Status.Equals("Active"));
                }
                else
                {
                    reviewsIQ = _context.Reviews.Include(x => x.Customer).Include(x => x.Restaurant).Where(x => x.RestaurantId == id && x.Status.Equals("Active"));
                }
                reviews = await PaginatedList<Review>.CreateAsync(reviewsIQ, pageIndex ?? 1, pageSize);

                List<Image> images = _context.Images.Where(x => x.RestaurantId == id).ToList();
                
                int totalreivews = _context.Reviews.Where(x => x.RestaurantId == id).Count();

                int star = reviews.Any() ? (int)Math.Ceiling((decimal)reviews.Average(x => x.Star)) : 0;

                List<string> timeList = new List<string>();
                for (int hour = 0; hour < 24; hour++)
                {
                    for (int minute = 0; minute < 60; minute += 30)
                    {
                        timeList.Add($"{hour:D2}:{minute:D2}");
                    }
                }

                List<string> newTimeList = new List<string>();


                foreach (string time in timeList)
                {
                    TimeSpan timeSpan = TimeSpan.Parse(time);
                    if (timeSpan >= restaurant.OpenTime && timeSpan <= restaurant.CloseTime)
                    {
                        newTimeList.Add(time);
                    }
                }

                int currentCustomerId = 0;
                string customerJson = HttpContext.Session.GetString("customer");
                if (!string.IsNullOrEmpty(customerJson))
                {
                    Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                    currentCustomerId = customerSession.CustomerId;
                }

                ViewBag.hasOrder = _context.Orders.Any(o => o.CustomerId == currentCustomerId && o.RestaurantId == id);
                ViewBag.Customer = _context.Customers.FirstOrDefault(x => x.CustomerId == currentCustomerId);
                ViewBag.Images = images;
                ViewBag.ImagesCount = images.Count();
                ViewBag.Star = star;
                ViewBag.Reviews = reviews;
                ViewBag.TotalReviews = totalreivews;
                ViewBag.NewTimeList = newTimeList;

                ViewBag.InforOrder = _context.Orders.Include(x => x.Restaurant).Include(x => x.Customer).FirstOrDefault(x => x.OrderId.ToString().Equals(TempData["InforOrderSuccess"]));
                ViewBag.MessSuccess = TempData["MessSuccess"];
                ViewBag.MessError = TempData["MessError"];
                return View(restaurant);
            }
            return RedirectToAction("Home");
        }


        [HttpPost("restaurant")]
        public async Task<IActionResult> Restaurant(int idRes, string name, string phone, DateTime date,
            TimeSpan ordertime, int numofperson, int numofchild, string content, string service, int star)
        {
            ViewBag.IsLogin = checkSession();
            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");
            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }
            Customer customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);

            if (customer != null)
            {   

                if (service.Equals("CreateReviews"))
                {
                    Review review = new Review()
                    {
                        RateId = 0,
                        RestaurantId = idRes,
                        CustomerId = customer.CustomerId,
                        Content = content,
                        Star = star,
                        CreateDate = DateTime.Now,
                        Status = "Active"
                    };
                    await _context.Reviews.AddAsync(review);
                    if (await _context.SaveChangesAsync() > 0)
                    {
                        TempData["MessSuccess"] = "Đánh giá nhà hàng thành công!";
                    }
                    else
                    {
                        TempData["MessError"] = "Đánh giá bị lỗi!";
                    }
                    return RedirectToAction("Restaurant", new { id = idRes });
                }

                if (service.Equals("CreateOrder"))
                {

                    Order order = new Order()
                    {
                        OrderId = 0,
                        RestaurantId = idRes,
                        CustomerId = customer.CustomerId,
                        NameReceiver = name,
                        PhoneReceiver = phone,
                        Date = date,
                        Time = ordertime,
                        NumberChild = numofchild,
                        NumberPerson = numofperson,
                        Content = content,
                        CreateDate = DateTime.Now,
                        Statusorder = "Wait"
                    };

                    if (_context.Orders != null)
                    {
                        await _context.Orders.AddAsync(order);
                        if (await _context.SaveChangesAsync() > 0)
                        {   
                            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                            var logger = loggerFactory.CreateLogger<SendMailService>();
                            Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                            sendMailService.SendEmailAsync(customer.Email, "Order", order.OrderId.ToString());
                            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == idRes);
                            sendMailService.SendEmailAsync(restaurant.Email, "New Order", order.OrderId.ToString());
                            TempData["InforOrderSuccess"] = order.OrderId.ToString();
                        }
                        else
                        {
                            TempData["MessError"] = "Đơn hàng đặt bị lỗi!";
                        }
                        return RedirectToAction("Restaurant", new { id = idRes });
                    }
                }
            }
            return NotFound();
        }

        // ABOUT VIEW
        [HttpGet("about")]
        public IActionResult About()
        {
            ViewBag.IsLogin = checkSession();
            return View();
        }

        [HttpGet("termsandcondition")]
        public IActionResult TermsAndCondition()
        {
            ViewBag.IsLogin = checkSession();
            return View();
        }


        [HttpGet("privacypolicy")]
        public IActionResult PrivacyPolicy()
        {
            ViewBag.IsLogin = checkSession();
            return View();
        }

        [HttpGet("blog")]
        public async Task<IActionResult> Blog(int? pageIndex, int? bloggroupid, string? service)
        {
            ViewBag.IsLogin = checkSession();
            ViewBag.bloggroupid = bloggroupid;
            ViewBag.service = service;
            if (_context.Blogs != null && _context.BlogGroups != null)
            {
                IQueryable<Blog> blogs = null;
                if (service != null && service.Equals("bloggroup"))
                {
                    blogs = _context.Blogs.Include(x => x.Bloggroup).Include(x => x.Admin)
                        .Where(x => x.BloggroupId == bloggroupid && x.Status.Equals("Active")).OrderByDescending(x => x.BlogId);
                }
                else
                {
                    blogs = _context.Blogs.Include(x => x.Bloggroup).Include(x => x.Admin).Where(x => x.Status.Equals("Active")).OrderByDescending(x => x.BlogId);
                }


                int pageSize = 6;
                PaginatedList<Blog> blog = await PaginatedList<Blog>.CreateAsync(blogs.AsNoTracking(), pageIndex ?? 1, pageSize);

                return View(blog);
            }
            return BadRequest();
        }

        [HttpGet("blog/detail")]
        public IActionResult BlogDetail(int? id)
        {
            ViewBag.IsLogin = checkSession();
            if (id == 0 || id == null || _context.Blogs == null)
            {
                return BadRequest();
            }
            else
            {
                Blog blog = _context.Blogs.Include(x => x.Admin).Include(x => x.Bloggroup).FirstOrDefault(x => x.BlogId == id && x.Status.Equals("Active"));

                List<Blog> blogs = _context.Blogs.Include(x => x.Admin).Include(x => x.Bloggroup).Where(x => x.BlogId != id && x.Status.Equals("Active"))
                    .OrderByDescending(x => x.BlogId).Take(3).ToList();

                ViewBag.NewBlog = blogs;
                ViewBag.NewBlogCount = blogs.Count();
                ViewBag.BlogGroups = _context.BlogGroups.ToList();

                return View(blog);
            }
        }

        // Conact VIEW
        [HttpGet("contact")]
        public IActionResult Contact()
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == true)
            {
                int idcus = 0;
                string customerJson = HttpContext.Session.GetString("customer");
                if (!string.IsNullOrEmpty(customerJson))
                {
                    Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                    idcus = customerSession.CustomerId;
                }
                ViewBag.Customer = _context.Customers.FirstOrDefault(x => x.CustomerId == idcus);
            }
            return View();
        }

        [HttpPost("contact")]
        public async Task<IActionResult> Contact(string name, string email, string phone, string subject, string content)
        {   
            ViewBag.IsLogin = checkSession();
            if (_context.Contacts != null)
            {
                Contact contact = new Contact()
                {   
                    Name = name,
                    Email = email,
                    Phone = phone,
                    Topic = subject,
                    Content = content,
                    ContactDate = DateTime.Now,
                    Status = "Active"
                };

                await _context.Contacts.AddAsync(contact);
                if (await _context.SaveChangesAsync() > 0)
                {
                    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    var logger = loggerFactory.CreateLogger<SendMailService>();
                    Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                    sendMailService.SendEmailAsync(email, "Contact", "Cám ơn bạn đã liên hệ với chúng tôi. Chúng tôi sẽ phản hồi lại với bạn sớm nhất có thể!");
                }
            }
            return RedirectToAction("Contact");
        }



        [HttpGet("orderdetail")]
        public IActionResult OrderDetail(int ?id)
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }

            if (id == null || id == 0)
            {
                return BadRequest();
            }

            int idcus = 0;
            string customerJson = HttpContext.Session.GetString("customer");
            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                idcus = customerSession.CustomerId;
            }
            ViewBag.Customer = _context.Customers.FirstOrDefault(x => x.CustomerId == idcus);
            Order order = _context.Orders.Include(x => x.Customer).Include(x => x.Restaurant).FirstOrDefault(x => x.CustomerId == idcus && x.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet("wishlist")]
        public async Task<IActionResult> Wishlist(int? pageIndex)
        {   
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");
            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }

            ViewBag.Customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);

            ViewBag.WishlistCount = _context.Wishlists.Where(x => x.CustomerId == id).Count();
            if (ViewBag.WishlistCount == 0)
            {
                return View();
            }

            IQueryable<WishlistDTO> wishlistDTOs = _context.Wishlists
                .Include(x => x.Customer)
                .Include(x => x.Restaurant)
                .Where(x => x.CustomerId == id && x.Restaurant.Status == "Active")
                .OrderByDescending(x => x.WishlistId)
                .Select(x => new WishlistDTO
                {
                    WishlistId = x.WishlistId,
                    ResId = x.Restaurant.ResId,
                    Image = x.Restaurant.Image,
                    NameRes = x.Restaurant.NameRes,
                    CategoryName = x.Restaurant.Category.CategoryName,
                    TotalReviews = _context.Reviews.Count(r => r.RestaurantId == x.Restaurant.ResId),
                    Star = (int)Math.Ceiling(_context.Reviews.Where(r => r.RestaurantId == x.Restaurant.ResId).DefaultIfEmpty().Average(r => (int?)r.Star ?? 0))
                });

            int pageSize = 6;
            PaginatedList<WishlistDTO> wishlists = await PaginatedList<WishlistDTO>.CreateAsync(wishlistDTOs.AsNoTracking(), pageIndex ?? 1, pageSize);

            return View(wishlists);
        }

        [HttpGet("createwishlist")]
        public async Task<IActionResult> CreateWishlist(int ?idRes, string page, int idfvr)
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }

            if(idRes == null || idRes == 0)
            {
                return BadRequest();
            }

            Restaurant restaurant = _context.Restaurants.FirstOrDefault(x => x.ResId == idRes);

            if (restaurant == null)
            {
                return BadRequest();
            }

            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");
            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }
            Customer customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);

            if (customer == null)
            {
                return BadRequest();
            }

            Wishlist existingWishlist = _context.Wishlists.FirstOrDefault(w => w.CustomerId == customer.CustomerId && w.RestaurantId == restaurant.ResId);
            if (existingWishlist != null)
            {
                TempData["MessError"] = "Cửa hàng đã ở trong mục yêu thích của bạn!";
                if (page.Equals("Home"))
                {
                    return RedirectToAction("Home");
                }
                if (page.Equals("Restaurant"))
                {
                    return RedirectToAction("Restaurant", new { id = idfvr });
                }
                if (page.Equals("Service"))
                {
                    return RedirectToAction("Service");
                }
            }

            Wishlist wishlist = new Wishlist()
            {
                WishlistId = 0,
                RestaurantId = restaurant.ResId,
                CustomerId = customer.CustomerId,
            };

            _context.Wishlists.Add(wishlist);
            if (await _context.SaveChangesAsync() > 0)
            {
                TempData["MessSuccess"] = "Đã thêm vào yêu thích!";
            }
            else
            {
                TempData["MessError"] = "Thêm vào yêu thích bị lỗi!";
            }
            if (page.Equals("Home"))
            {
                return RedirectToAction("Home");
            }
            if (page.Equals("Restaurant"))
            {
                return RedirectToAction("Restaurant", new { id = idfvr });
            }
            if (page.Equals("Service"))
            {
                return RedirectToAction("Service");
            }
            return RedirectToAction("Home");
        }


        [HttpGet("orderhistory")]
        public async Task<IActionResult> OrderHistory(int? pageIndex, string service, int idOrder)
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");

            if (!string.IsNullOrEmpty(service))
            {
                if (service.Equals("updateStatus"))
                {
                    Order order = _context.Orders.Include(x => x.Restaurant).FirstOrDefault(x => x.OrderId == idOrder);
                    order.Statusorder = "Cancel";
                    _context.Orders.Update(order);
                    if (await _context.SaveChangesAsync() > 0)
                    {
                        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                        var logger = loggerFactory.CreateLogger<SendMailService>();
                        Mail.SendMailService sendMailService = new Mail.SendMailService(Options.Create(mailSettings), logger);
                        sendMailService.SendEmailAsync(order.Restaurant.Email, "UpdateOrderStatusRestaurant", order.OrderId.ToString());
                    }
                }
            }

            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }

            ViewBag.Customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);

            IQueryable<Models.Order> ordersIQ = _context.Orders
                .Include(x => x.Customer)
                .Include(x => x.Restaurant)
                .Where(x => x.CustomerId == id)
                .OrderByDescending(x => x.OrderId);

            int pageSize = 8;
            List<Models.Order> orders =
                await PaginatedList<Models.Order>.CreateAsync(ordersIQ.AsNoTracking(), pageIndex ?? 1, pageSize);

            return View(orders);
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> Review(int? pageIndex)
        {
            ViewBag.IsLogin = checkSession();
            if (checkSession() == false)
            {
                return RedirectToAction("Login");
            }
            int id = 0;
            string customerJson = HttpContext.Session.GetString("customer");

            if (!string.IsNullOrEmpty(customerJson))
            {
                Customer customerSession = JsonConvert.DeserializeObject<Customer>(customerJson);
                id = customerSession.CustomerId;
            }
            ViewBag.Customer = _context.Customers.FirstOrDefault(x => x.CustomerId == id);

            IQueryable<Models.Review> reviewsIQ = _context.Reviews
                .Include(x => x.Customer)
                .Include(x => x.Restaurant)
                .Where(x => x.CustomerId == id)
                .OrderByDescending(x => x.RateId);

            int pageSize = 3;
            List<Models.Review> reviews = 
                await PaginatedList<Models.Review>.CreateAsync(reviewsIQ.AsNoTracking(), pageIndex ?? 1, pageSize);

            return View(reviews);
        }

    }
}
