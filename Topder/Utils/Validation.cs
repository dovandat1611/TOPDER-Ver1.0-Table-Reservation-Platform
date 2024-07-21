using Topder.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Topder.Utils
{
    public class Validation
    {
        public static bool IsUsernameUnique(string username, TopderContext context)
        {
            return !context.Customers.Any(c => c.Username == username);
        }

        public static bool IsUsernameAdmin(string username, TopderContext context)
        {
            return !context.Admins.Any(c => c.Username == username);
        }

        public static bool IsUsernameRestaurant(string username, TopderContext context)
        {
            return !context.Restaurants.Any(c => c.Username == username);
        }

        public static bool IsEmailValid(string email)
        {
            string emailPattern = @"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$";
            Regex regex = new Regex(emailPattern);
            return regex.IsMatch(email);
        }

        public static bool IsPasswordValid(string password)
        {
            return Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$");
        }

        public static string GenerateOTP(int length)
        {
            const string chars = "0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        //public static bool IsPrice(double? price)
        //{
        //    if (price > 0)
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        //public static bool IsDiscount(double? discount)
        //{
        //    if (discount >= 0 && discount <= 1)
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        //public static string ConvertMonthNumberToName(string monthNumber)
        //{
        //    int monthInt = int.Parse(monthNumber);
        //    string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthInt);
        //    return monthName;
        //}


        //public static string HashPassword(string password)
        //{
        //    try
        //    {
        //        using (SHA256 sha256 = SHA256.Create())
        //        {
        //            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

        //            StringBuilder sb = new StringBuilder();
        //            foreach (byte b in hashedBytes)
        //            {
        //                sb.Append(b.ToString("x2"));
        //            }
        //            return sb.ToString();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //    return null;
        //}


    }
}
