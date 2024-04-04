using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }
        public class PasswordManager
        {
            // Mã hóa mật khẩu bằng bcrypt
            public static string HashPassword(string password)
            {
                return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt());
            }

            // Kiểm tra mật khẩu đã nhập với mật khẩu đã mã hóa bằng bcrypt
            public static bool VerifyPassword(string enteredPassword, string hashedPassword)
            {
                return BCrypt.Net.BCrypt.Verify(enteredPassword, hashedPassword);
            }
        }
    }
}