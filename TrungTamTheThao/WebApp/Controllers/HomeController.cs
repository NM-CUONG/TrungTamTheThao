using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Services.Description;
using WebApp.Models;
using Newtonsoft.Json;
using Microsoft.SqlServer.Server;
using System.Data.Entity.Migrations;
using System.Web.WebPages;
using System.Web.UI.WebControls;
using WebApp.Constant;
using System.ComponentModel;

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

    // Chuyển ENUM thành droplist

    public static class SelectListHelper
    {
        public static List<SelectListItem> ToSelectList<T>(this IEnumerable<T> source, Func<T, string> textSelector, Func<T, string> valueSelector)
        {
            return source.Select(item => new SelectListItem
            {
                Text = textSelector(item),
                Value = valueSelector(item)
            }).ToList();
        }
    }


    //Các chức năng
    public class HomeController : Controller
    {
        //đối tượng ánh xạ CSDL
        private readonly DBContext db = new DBContext();

        //Cấu hình email
        private static string EmailHost = "smtp.gmail.com";
        private static string EmailPort = "587";
        private static string EmailFrom = "nguyenmanhcuong2k2.hsbg@gmail.com";
        private static string EmailFromPassword = "amyr nbnb akdc djgc";

        public ActionResult Index()
        {
            return View();
        }

        //Check trùng UserName
        public bool CheckExistsUserName(string UserName)
        {
            return db.tb_User.Any(x => x.UserName == UserName);
        }
        //Đăng ký
        public ActionResult Regis()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Regis(tb_User model)
        {

            //Check trùng username
            if (CheckExistsUserName(model.UserName))
            {
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
            }
            //Sinh userid tự động
            var lastItem = db.tb_User.OrderByDescending(x => x.ID).FirstOrDefault();
            if (lastItem != null)
            {
                model.UserID = "U" + lastItem.ID;
            }
            else
            {
                model.UserID = "U00";
            }
            model.Status = 0;
            // Mã hóa mật khẩu trước khi lưu
            model.Password = PasswordManager.HashPassword(model.Password);
            //Lưu
            db.tb_User.Add(model);
            db.SaveChanges();

            if (!SendMail(model.Email, model.UserID))
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

        public ActionResult Logout()
        {
            Session.Remove("UserInfor");
            return View("Login");
        }

        //Hiển thị màn hình đăng nhập
        public ActionResult Login()
        {
            return View();
        }

        // Xử lý đăng nhập
        [HttpPost]
        public JsonResult Login(tb_User model)
        {
            var account = db.tb_User.Where(x => x.UserName == model.UserName).FirstOrDefault();
            if (account != null)
            {
                try
                {
                    if (PasswordManager.VerifyPassword(model.Password, account.Password))
                    {
                        if (account.Status == 0)
                        {
                            return Json(new { success = false, message = "Tài khoản chưa được kích hoạt, vui lòng kiểm tra email!" });
                        }
                        Session["UserInfor"] = account;
                        return Json(new { success = true, });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Đăng nhập không thành công!" + ex.Message });
                }

            }
            else
            {
                return Json(new { success = false, message = "Tên tài khoản không tồn tại" });
            }
            return Json(new { success = false, message = "Tài khoản hoặc mật khẩu không chính xác!" });
        }

        [HttpGet]
        public JsonResult getEmptyShift(string arenaId, string cateId, DateTime ngayBatDau, DateTime ngayKetThuc)
        {
            // Lấy ra tất cả sân cầu lông chưa được xác nhận đặt
            var listBooked = db.tb_Booking.Where(x => x.ArenaID == arenaId && x.Status == 0).ToList();
            //Lấy ra tất cả khung giờ của sân cầu lông
            var listShift = db.tb_Shift.Where(x => x.CateID == cateId).ToList();
            //Tạo list sân rỗng chưa đặt
            List<tb_Shift> listEmptyShift = new List<tb_Shift>();
            //Tạo list sân rỗng đã đặt
            List<tb_Shift> listBookedShift = new List<tb_Shift>();


            //Chọn ra những sân đã có người sử dụng trong khung giờ đó
            foreach (var item in listBooked)
            {
                if (!(ngayKetThuc < item.StartTime || ngayBatDau > item.EndTime))
                {
                    listBookedShift.Add(listShift.Find(x => x.ShiftID == item.ShiftID));
                }
            }

            listEmptyShift = listShift
            .Where(shift => !listBookedShift.Any(bookedShift => bookedShift.ShiftID == shift.ShiftID))
            .Select(x => new tb_Shift { ShiftID = x.ShiftID, ShiftName = x.ShiftName, Price = x.Price })
            .ToList();

            return Json(new { listEmptyShift }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult getEmptyShift2(string arenaId, DateTime ngayBatDau, DateTime ngayKetThuc)
        {
            int? maxPerson = db.tb_Arena.Where(x => x.ArenaID == arenaId).FirstOrDefault().MaxPersons;

            if (maxPerson == null)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi truy vấn CSDL!" });
            }

            var listBooked = db.tb_Booking.Where(x => x.ArenaID == arenaId).ToList();
            var persons = 0;

            foreach (var item in listBooked)
            {
                if (!(ngayKetThuc < item.StartTime || ngayBatDau > item.EndTime))
                {
                    persons++;

                }
                else
                {
                    continue;

                }
            }

            if (persons >= maxPerson)
            {
                return Json(new { success = false, message = "Đã đủ người!" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Vẫn còn chỗ trống" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult BookingSwimming()
        {
            List<tb_Arena> Swimmings = db.tb_Arena.Where(x => x.CateID == "swimming").ToList();
            ViewBag.Swimmings = Swimmings;
            return View();
        }

        public ActionResult BookingGym()
        {
            List<tb_Arena> Gyms = db.tb_Arena.Where(x => x.CateID == "gym").ToList();
            ViewBag.Gyms = Gyms;
            return View();
        }

        public ActionResult BookingFootball()
        {
            List<tb_Arena> Footballs = db.tb_Arena.Where(x => x.CateID == "football").ToList();
            ViewBag.Footballs = Footballs;
            return View();
        }
        public ActionResult BookingBadminton()
        {

            List<tb_Arena> Badmintons = db.tb_Arena.Where(x => x.CateID == "badminton").ToList();
            ViewBag.Badmintons = Badmintons;
            return View();
        }
        public ActionResult GetFormBookingSwimming(string arenaID)
        {
            tb_Arena swimming = db.tb_Arena.Where(x => x.ArenaID == arenaID).FirstOrDefault();
            tb_Shift khungGioSwimming = db.tb_Shift.Where(x => x.CateID == "swimming").FirstOrDefault();

            ViewBag.khungGioSwimming = khungGioSwimming;
            return PartialView("_BookingSwimmingPartial", swimming);
        }
        public ActionResult GetFormBookingGym(string arenaID)
        {
            tb_Arena Gym = db.tb_Arena.Where(x => x.ArenaID == arenaID).FirstOrDefault();
            tb_Shift khungGioGym = db.tb_Shift.Where(x => x.CateID == "gym").FirstOrDefault();

            ViewBag.khungGioGym = khungGioGym;
            return PartialView("_BookingGymPartial", Gym);
        }
        public ActionResult GetFormBookingBadminton(string arenaID)
        {
            tb_Arena Badminton = db.tb_Arena.Where(x => x.ArenaID == arenaID).FirstOrDefault();
            return PartialView("_BookingBadmintonPartial", Badminton);
        }

        public ActionResult GetFormBookingFootball(string arenaID)
        {
            tb_Arena Badminton = db.tb_Arena.Where(x => x.ArenaID == arenaID).FirstOrDefault();
            return PartialView("_BookingFootballPartial", Badminton);
        }

        [HttpPost]
        public JsonResult HandleBookingBadminton(tb_Booking model)
        {
            tb_Booking lastBooking = db.tb_Booking.OrderByDescending(b => b.ID).FirstOrDefault();

            if (lastBooking != null)
            {
                model.BookingID = "B" + (lastBooking.ID + 1);
            }
            else
            {
                model.BookingID = "B0";
            }

            if (model.isCoDinh == 0)
            {
                model.StartTime = model.ngaySuDung;
                model.EndTime = model.ngaySuDung;
            }

            tb_User userInfor = Session["UserInfor"] as tb_User;
            if (userInfor != null)
            {
                model.UserID = userInfor.UserID;
            }

            model.Status = 0;

            // lưu vào csdl

            try
            {
                db.tb_Booking.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đặt sân không thành công!, Có lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult HandleBookingFootball(tb_Booking model)
        {
            tb_Booking lastBooking = db.tb_Booking.OrderByDescending(b => b.ID).FirstOrDefault();

            if (lastBooking != null)
            {
                model.BookingID = "B" + (lastBooking.ID + 1);
            }
            else
            {
                model.BookingID = "B0";
            }

            if (model.isCoDinh == 0)
            {
                model.StartTime = model.ngaySuDung;
                model.EndTime = model.ngaySuDung;
            }

            tb_User userInfor = Session["UserInfor"] as tb_User;
            if (userInfor != null)
            {
                model.UserID = userInfor.UserID;
            }

            model.Status = 0;

            // lưu vào csdl

            try
            {
                db.tb_Booking.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đặt sân không thành công!, Có lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult HandleBookingGym(tb_Booking model)
        {
            tb_Booking lastBooking = db.tb_Booking.OrderByDescending(b => b.ID).FirstOrDefault();

            if (lastBooking != null)
            {
                model.BookingID = "B" + (lastBooking.ID + 1);
            }
            else
            {
                model.BookingID = "B0";
            }

            if (model.isCoDinh == 0)
            {
                model.StartTime = model.ngaySuDung;
                model.EndTime = model.ngaySuDung;
            }

            tb_User userInfor = Session["UserInfor"] as tb_User;
            if (userInfor != null)
            {
                model.UserID = userInfor.UserID;
            }

            model.Status = 0;

            // lưu vào csdl

            try
            {
                db.tb_Booking.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đặt sân không thành công!, Có lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult HandleBookingSwimming(tb_Booking model)
        {
            tb_Booking lastBooking = db.tb_Booking.OrderByDescending(b => b.ID).FirstOrDefault();

            if (lastBooking != null)
            {
                model.BookingID = "B" + (lastBooking.ID + 1);
            }
            else
            {
                model.BookingID = "B0";
            }

            if (model.isCoDinh == 0)
            {
                model.StartTime = model.ngaySuDung;
                model.EndTime = model.ngaySuDung;
            }

            tb_User userInfor = Session["UserInfor"] as tb_User;
            if (userInfor != null)
            {
                model.UserID = userInfor.UserID;
            }

            model.Status = 0;

            // lưu vào csdl

            try
            {
                db.tb_Booking.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đặt bể bơi không thành công!, Có lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Payment()
        {
            return View();
        }

        // Hàm xem lịch sử booking
        public ActionResult HistoriesBooking()
        {
            tb_User Account = Session["UserInfor"] as tb_User;

            if (Account == null)
            {
                ViewBag.Error = "Xảy ra lỗi trong quá trình truy vấn!";
                return View();
            }

            try
            {
                List<tb_Booking> historiesBooking = db.tb_Booking.Where(x => x.UserID == Account.UserID).ToList();
                ViewBag.historiesBooking = historiesBooking;
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Xảy ra lỗi trong quá trình truy vấn!" + ex.Message;
                return View();
            }

            return View();
        }

        public ActionResult CanCelBooking(string BookingID)
        {

            if (BookingID == null)
            {
                ViewBag.Error = "Đã có lỗi xảy ra trong quá trình truy vấn";
                return View("HistoriesBooking");
            }

            db.tb_Booking.Where(x => x.ID.ToString() == BookingID).FirstOrDefault().Status = 2;
            db.SaveChanges();

            tb_User Account = Session["UserInfor"] as tb_User;

            if (Account == null)
            {
                ViewBag.Error = "Xảy ra lỗi trong quá trình truy vấn!";
                return View();
            }

            try
            {
                List<tb_Booking> historiesBooking = db.tb_Booking.Where(x => x.UserID == Account.UserID).ToList();
                ViewBag.historiesBooking = historiesBooking;
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Xảy ra lỗi trong quá trình truy vấn!" + ex.Message;
                return View();
            }

            return View("HistoriesBooking");
        }

        public ActionResult Account()
        {
            tb_User Account = Session["UserInfor"] as tb_User;
            if (Account == null)
            {
                ViewBag.Error = "Không thể truy vấn thông tin tài khoản!";
                return View();
            }

            return View(Account);

        }
        [HttpPost]
        public JsonResult Account(tb_User model)
        {
            try
            {
                var user = db.tb_User.Where(x => x.ID == model.ID).FirstOrDefault();
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản trong CSDL!" });
                }
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.Address = model.Address;
                db.SaveChanges();
                Session["UserInfor"] = user;
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi: " + ex.Message });

            }

            return Json(new { success = true, message = "Cập nhật thông tin thành công!" }, JsonRequestBehavior.AllowGet);
        }

        [Route("/Home/ChangePassword/")]
        public ActionResult ChangePassword(long Id)
        {
            long? ID = Id as long?;
            if (ID == null)
            {
                ViewBag.Error = "Có lỗi trong quá trình đổi mật khẩu!";
                return View();
            }
            tb_User currentAccount = db.tb_User.Where(x => x.ID == ID).FirstOrDefault();
            if (currentAccount == null)
            {
                ViewBag.Error = "Có lỗi trong quá trình dổi mật khẩu!";
                return View();
            }
            return View(currentAccount);
        }
        [HttpPost]
        public ActionResult ChangePassword(tb_User model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Đổi mật khẩu không thành công, đã có lỗi xảy ra khi gửi dữ liệu đi!" });
            }
            tb_User Account = Session["UserInfor"] as tb_User;

            if (Account == null)
            {
                return Json(new { success = false, message = "Đổi mật khẩu không thành công, đã có lỗi xảy ra!" });
            }

            try
            {
                tb_User currentAccount = db.tb_User.Where(x => x.ID == Account.ID).FirstOrDefault();
                currentAccount.Password = PasswordManager.HashPassword(model.Password);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản!" + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, message = "Thay đổi mật khẩu thành công!" }, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public ActionResult getCapTra(string UserID)
        {
            if (string.IsNullOrEmpty(UserID))
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin tài khoản!" }, JsonRequestBehavior.AllowGet);

            }
            tb_User account = db.tb_User.Where(x => x.UserID == UserID).FirstOrDefault();
            if (account == null)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi!" }, JsonRequestBehavior.AllowGet);
            }
            try
            {

                using (MailMessage mm = new MailMessage(EmailFrom, account.Email))
                {
                    //Tiêu đề mail
                    mm.Subject = "Quên mật khẩu";
                    //Nội dung mail - gửi kèm link xác nhận có userid để nhận diện khi confirm
                    Random random = new Random();
                    var captra = random.Next(1000, 10000);
                    Session["captra"] = captra;
                    mm.Body = $"Mã captra của bạn là: " + captra;
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi trong quá trình lấy mã!" + ex.Message }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Vui lòng kiểm tra email để lấy mã xác nhận!" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ForgotPassword(string username)
        {
            var model = db.tb_User.Where(x => x.UserName == username).FirstOrDefault();
            if (model == null)
            {
                ViewBag.Error = "Không tìm thấy thông tin tài khoản!";
                return View();
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult ForgotPassword(FormCollection form)
        {
            string userID = form["UserID"];
            string passWord = form["Password"];
            string ct = form["captra"];


            if (string.IsNullOrEmpty(userID) ||
                string.IsNullOrEmpty(passWord) ||
                Session["captra"] == null ||
                string.IsNullOrEmpty(ct))
            {
                return Json(new { success = false, message = "Quên mật khẩu không thành công, đã có lỗi xảy ra!" }, JsonRequestBehavior.AllowGet);
            }

            var captra = (int)Session["captra"];
            if (captra.ToString() != ct)
            {
                return Json(new { success = false, message = "Mã xác nhận không đúng!" }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                tb_User tk = db.tb_User.Where(x => x.UserID == userID).FirstOrDefault();
                tk.Password = PasswordManager.HashPassword(passWord);
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Quên mật khẩu không thành công, đã có lỗi xảy ra!" }, JsonRequestBehavior.AllowGet);
            }

            Session.Remove("captra");
            return Json(new { success = true, message = "Quên mật khẩu thành công!" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ChangeEmail()
        {
            tb_User Account = Session["UserInfor"] as tb_User;

            if (Account == null)
            {
                ViewBag.Error = "Không tìm thấy thông tin tài khoản!";
                return View();
            }
            return View(Account);
        }



        [HttpPost]
        public ActionResult ChangeEmail(FormCollection form)
        {

            string newEmail = form["newEmail"];

            if (string.IsNullOrEmpty(newEmail))
            {
                return Json(new { success = false, message = "Có lỗi xảy ra trong quá trình thay đổi mail!" }, JsonRequestBehavior.AllowGet);
            }
            tb_User Account = Session["UserInfor"] as tb_User;

            if (Account == null)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra trong quá trình thay đổi mail!" }, JsonRequestBehavior.AllowGet);

            }
            try
            {
                using (MailMessage mm = new MailMessage(EmailFrom, Account.Email))
                {
                    //Tiêu đề mail
                    mm.Subject = "Xác nhận thay đổi email";
                    //Nội dung mail - gửi kèm link xác nhận có userid để nhận diện khi confirm
                    mm.Body = $"Để xác nhận thay đổi email vui lòng nhấp vào link sau <a href=\"https://localhost:44315/Home/ConfirmEmailChange/?userid={Account.ID}&newEmail={newEmail}\">Xác nhận thay đổi email</a>";
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra trong quá trình thay đổi email!" + ex.Message });

            }

            return Json(new { success = true, message = "Vui lòng kiểm tra email xác nhận để hoàn tất thay đổi Email!" });

        }

        public ActionResult ConfirmEmailChange(string userid, string newEmail)
        {
            var account = db.tb_User.Where(x => x.ID.ToString() == userid).FirstOrDefault();
            if (account != null)
            {
                account.Email = newEmail;
                db.SaveChanges();
                Session["UserInfor"] = account;
            }
            return View();
        }


        // Phần của admin

        #region Các hàm xử lý Role
        public ActionResult ManageRole()
        {
            List<tb_Role> listRole = db.tb_Role.ToList();
            ViewBag.listRole = listRole;
            return View();
        }

        public ActionResult GetTableRole()
        {
            List<tb_Role> listRole = db.tb_Role.ToList();
            ViewBag.listRole = listRole;
            return PartialView("_TableRolePartial");
        }

        public ActionResult CreateRole()
        {
            tb_Role role = new tb_Role();
            tb_Role lastRole = db.tb_Role.OrderByDescending(x => x.ID).FirstOrDefault();
            role.RoleID = "R" + (lastRole.ID + 1);

            return PartialView("_CreateRolePartial", role);
        }

        [HttpPost]
        public ActionResult CreateRole(tb_Role model)
        {
            try
            {
                db.tb_Role.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Thêm mới không thành công, lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Thêm mới thành công" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult EditRole(long ID)
        {
            tb_Role role = db.tb_Role.FirstOrDefault(x => x.ID == ID);
            return PartialView("_EditRolePartial", role);
        }

        [HttpPost]
        public ActionResult EditRole(tb_Role model)
        {
            try
            {
                var role = db.tb_Role.FirstOrDefault(x => x.ID == model.ID);
                role.RoleID = model.RoleID;
                role.RoleName = model.RoleName;
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Sửa bản ghi không thành công!" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Sửa bản ghi thành công!" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DeleteRole(long ID)
        {
            try
            {
                tb_Role model = db.tb_Role.Where(x => x.ID == ID).FirstOrDefault();
                db.tb_Role.Remove(model);
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã gặp lỗi khi xóa!" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, message = "Xóa bản ghi thành công" }, JsonRequestBehavior.AllowGet);

        }

        #endregion

        #region Các hàm xử lý User
        public ActionResult ManageUser()
        {
            List<tb_User> listUser = db.tb_User.ToList();
            ViewBag.listUser = listUser;

            return View();
        }

        public ActionResult GetTableUser()
        {
            List<tb_User> listUser = db.tb_User.ToList();

            foreach(var item in listUser)
            {
                item.RoleName = db.tb_Role.Where(x => x.RoleID == item.RoleID).FirstOrDefault().RoleName;
            }
            ViewBag.listUser = listUser;

           
            return PartialView("_TableUserPartial");
        }

        public ActionResult CreateUser()
        {
            tb_User User = new tb_User();
            tb_User lastUser = db.tb_User.OrderByDescending(x => x.ID).FirstOrDefault();
            User.UserID = "U" + (lastUser.ID + 1);

            var listRole = db.tb_Role.ToList();
            ViewBag.listRole = listRole.ToSelectList(r => r.RoleName, r => r.RoleID);
            ViewBag.listStatus = TrangThaiUserConstant.GetSelectListItems(-1);

            return PartialView("_CreateUserPartial", User);
        }

        [HttpPost]
        public ActionResult CreateUser(tb_User model)
        {
            try
            {
                model.Password = PasswordManager.HashPassword(model.Password);
                db.tb_User.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Thêm mới không thành công, lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Thêm mới thành công" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult EditUser(long ID)
        {
            var listRole = db.tb_Role.ToList();
            ViewBag.listRole = listRole.ToSelectList(r => r.RoleName, r => r.RoleID);
            ViewBag.listStatus = TrangThaiUserConstant.GetSelectListItems(-1);

            tb_User User = db.tb_User.FirstOrDefault(x => x.ID == ID);
            return PartialView("_EditUserPartial", User);
        }

        [HttpPost]
        public ActionResult EditUser(tb_User model)
        {
            try
            {
                var User = db.tb_User.FirstOrDefault(x => x.ID == model.ID);
                User.UserID = model.UserID;

                if (db.tb_User.Where(x => x.ID == model.ID && model.UserName != User.UserName).Any())
                {
                    return Json(new { success = false, message = "Tên tài khoản đã tồn tại!" }, JsonRequestBehavior.AllowGet);
                }

                User.UserName = model.UserName;
                if (!PasswordManager.VerifyPassword(model.Password, User.Password))
                {
                    User.Password = PasswordManager.HashPassword(model.Password);
                }
                User.FullName = model.FullName;
                User.Email = model.Email;
                User.Phone = model.Phone;
                User.Address = model.Address;
                User.Status = model.Status;
                User.RoleID = model.RoleID;

                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Sửa bản ghi không thành công!" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Sửa bản ghi thành công!" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DeleteUser(long ID)
        {
            try
            {
                tb_User model = db.tb_User.Where(x => x.ID == ID).FirstOrDefault();
                db.tb_User.Remove(model);
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã gặp lỗi khi xóa!" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, message = "Xóa bản ghi thành công" }, JsonRequestBehavior.AllowGet);

        }

        #endregion

    }
}