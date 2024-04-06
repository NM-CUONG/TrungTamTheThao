using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using WebApp.Models;


namespace WebApp.Controllers
{

    //Mã hóa mật khẩu
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
    //Các chức năng
    public class HomeController : Controller
    {
        //đối tượng ánh xạ CSDL
        private readonly DBContext db = new DBContext();

        //Cấu hình email
        private static string EmailHost = "smtp.gmail.com";
        private static string EmailPort= "587";
        private static string EmailFrom= "nguyenmanhcuong2k2.hsbg@gmail.com";
        private static string EmailFromPassword= "amyr nbnb akdc djgc";

        public ActionResult Index()
        {
            return View();
        }

        //Check trùng UserName
        public bool CheckExistsUserName(string UserName)
        {
            return db.tb_User.Any( x => x.UserName == UserName);
        }
        //Đăng ký
        public ActionResult Regis() 
        {
            return View ();
        }

        [HttpPost]
        public JsonResult Regis(tb_User model)
        {

            //Check trùng username
            if (CheckExistsUserName(model.UserName))
            {
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại"});
            }
            //Sinh userid tự động
            var lastItem = db.tb_User.OrderByDescending(x => x.ID).FirstOrDefault();
            model.UserID = "U" + lastItem.ID;
            // Mã hóa mật khẩu trước khi lưu
            model.Password = PasswordManager.HashPassword(model.Password);
            //Lưu
            db.tb_User.Add(model);
            db.SaveChanges();

            if(!SendMail(model.Email, model.UserID))
            {
                return Json(new { success = false, message = "Không thể gửi email xác nhận đăng ký" });
            }

            return Json(new { success = true, message = "Vui lòng truy cập email để hoàn tất đăng ký tài khoản" });
        }

        //Gửi Email
        public bool SendMail(string EmailTo, string UserID)
        {
            try
            {
                using (MailMessage mm = new MailMessage(EmailFrom, EmailTo))
                {
                    //Tiêu đề mail
                    mm.Subject = "Xác nhận đăng ký tài khoản";
                    //Nội dung mail - gửi kèm link xác nhận có userid để nhận diện khi confirm
                    mm.Body = $"Để xác nhận tài khoản vui lòng nhấp vào link sau <a href=\"https://localhost:44315/Home/ConfirmEmailRegis/?userid={UserID}\">Xác nhận đăng ký tài khoản</a>";
                    mm.IsBodyHtml = true;
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = EmailHost;
                        smtp.EnableSsl = true;

                        //tài khoản đăng kí sử dụng smtp
                        NetworkCredential cred = new NetworkCredential(EmailFrom, EmailFromPassword);
                        smtp.UseDefaultCredentials = true;
                        smtp.Credentials = cred;
                        smtp.Port = 587;
                        smtp.Send(mm);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        //Xác nhận email đăng ký tài khoản
        public ActionResult ConfirmEmailRegis(string userid)
        {
            //lay ra account vua dang ky de xac nhan bang cach chinh status = 1
            var account = db.tb_User.Where(x => x.UserID == userid).FirstOrDefault();
            if (account != null)
            {
                account.Status = 1;
                db.SaveChanges();
            }
            return View();
        }

        //Hiển thị màn hình đăng nhập
        public ActionResult Login()
        {
            return View();
        }

        // Xử lý đăng nhập
        [HttpPost]
        public ActionResult Login(tb_User model)
        {
            var account = db.tb_User.Where(x => x.UserName == model.UserName).FirstOrDefault();
            if (account != null)
            {
                //Giải mã mật khẩu
                if (PasswordManager.VerifyPassword(model.Password, account.Password))
                {
                    return Redirect("/Home/Index");
                }
            }
            return View();
        }

    }

}