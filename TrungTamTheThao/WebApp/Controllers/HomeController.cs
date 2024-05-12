﻿using System;
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
using System.IO;
using UnidecodeSharpFork;
using System.Security.Policy;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using VNPAYAPI.Areas.VNPayAPI.Util;

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
        private static int EmailPort = 587;
        private static string EmailFrom = "nguyenmanhcuong2k2.hsbg@gmail.com";
        private static string EmailFromPassword = "amyr nbnb akdc djgc";

        //Cấu hình thanh toán online vnpay test
        public string url = "http://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public string returnUrl = $"https://localhost:{44315}/vnpayAPI/PaymentConfirm";
        public string tmnCode = "5YMDVWOK";
        public string hashSecret = "38GCYMT92OXRPGTDFZ6JTA00MXIPU8BZ";

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

            //Check trùng shiftname
            if (CheckExistsUserName(model.UserName))
            {
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
            }
            //Sinh shiftid tự động
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
                    //Nội dung mail - gửi kèm link xác nhận có shiftid để nhận diện khi confirm
                    mm.Body = $"Để xác nhận tài khoản vui lòng nhấp vào link sau <a href=\"https://localhost:44315/Home/ConfirmEmailRegis/?shiftid={UserID}\">Xác nhận đăng ký tài khoản</a>";
                    mm.IsBodyHtml = true;
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = EmailHost;
                        smtp.EnableSsl = true;

                        //tài khoản đăng kí sử dụng smtp
                        NetworkCredential cred = new NetworkCredential(EmailFrom, EmailFromPassword);
                        smtp.UseDefaultCredentials = true;
                        smtp.Credentials = cred;
                        smtp.Port = EmailPort;
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
        public ActionResult ConfirmEmailRegis(string shiftid)
        {
            //lay ra account vua dang ky de xac nhan bang cach chinh status = 1
            var account = db.tb_User.Where(x => x.UserID == shiftid).FirstOrDefault();
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

            tb_User shiftInfor = Session["UserInfor"] as tb_User;
            if (shiftInfor != null)
            {
                model.UserID = shiftInfor.UserID;
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

            tb_User shiftInfor = Session["UserInfor"] as tb_User;
            if (shiftInfor != null)
            {
                model.UserID = shiftInfor.UserID;
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

            tb_User shiftInfor = Session["UserInfor"] as tb_User;
            if (shiftInfor != null)
            {
                model.UserID = shiftInfor.UserID;
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

            tb_User shiftInfor = Session["UserInfor"] as tb_User;
            if (shiftInfor != null)
            {
                model.UserID = shiftInfor.UserID;
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

        public ActionResult Process(FormCollection form)
        {
            string amount = form["amount"];
            string infor = "Trả tiền đặt sân";
            string orderinfor = form["customerName"];
            string hostName = System.Net.Dns.GetHostName();
            string clientIPAddress = System.Net.Dns.GetHostAddresses(hostName).GetValue(0).ToString();
            PayLib pay = new PayLib();

            pay.AddRequestData("vnp_Version", "2.1.0"); //Phiên bản api mà merchant kết nối. Phiên bản hiện tại là 2.1.0
            pay.AddRequestData("vnp_Command", "pay"); //Mã API sử dụng, mã cho giao dịch thanh toán là 'pay'
            pay.AddRequestData("vnp_TmnCode", tmnCode); //Mã website của merchant trên hệ thống của VNPAY (khi đăng ký tài khoản sẽ có trong mail VNPAY gửi về)
            pay.AddRequestData("vnp_Amount", amount); //số tiền cần thanh toán, công thức: số tiền * 100 - ví dụ 10.000 (mười nghìn đồng) --> 1000000
            pay.AddRequestData("vnp_BankCode", ""); //Mã Ngân hàng thanh toán (tham khảo: https://sandbox.vnpayment.vn/apis/danh-sach-ngan-hang/), có thể để trống, người dùng có thể chọn trên cổng thanh toán VNPAY
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")); //ngày thanh toán theo định dạng yyyyMMddHHmmss
            pay.AddRequestData("vnp_CurrCode", "VND"); //Đơn vị tiền tệ sử dụng thanh toán. Hiện tại chỉ hỗ trợ VND
            pay.AddRequestData("vnp_IpAddr", clientIPAddress); //Địa chỉ IP của khách hàng thực hiện giao dịch
            pay.AddRequestData("vnp_Locale", "vn"); //Ngôn ngữ giao diện hiển thị - Tiếng Việt (vn), Tiếng Anh (en)
            pay.AddRequestData("vnp_OrderInfo", infor); //Thông tin mô tả nội dung thanh toán
            pay.AddRequestData("vnp_OrderType", "other"); //topup: Nạp tiền điện thoại - billpayment: Thanh toán hóa đơn - fashion: Thời trang - other: Thanh toán trực tuyến
            pay.AddRequestData("vnp_ReturnUrl", returnUrl); //URL thông báo kết quả giao dịch khi Khách hàng kết thúc thanh toán
            pay.AddRequestData("vnp_TxnRef", orderinfor); //mã hóa đơn

            string paymentUrl = pay.CreateRequestUrl(url, hashSecret);
            return new RedirectResult(paymentUrl);
        }

        public bool ValidateSignature(string rspraw, string inputHash, string secretKey)
        {
            string myChecksum = PayLib.HmacSHA512(secretKey, rspraw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        // GET: Payment/PaymentResult
        public ActionResult PaymentConfirm()
        {
            // Hiển thị kết quả thanh toán cho người dùng
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
                var shift = db.tb_User.Where(x => x.ID == model.ID).FirstOrDefault();
                if (shift == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản trong CSDL!" });
                }
                shift.FullName = model.FullName;
                shift.Phone = model.Phone;
                shift.Address = model.Address;
                db.SaveChanges();
                Session["UserInfor"] = shift;
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
                    //Nội dung mail - gửi kèm link xác nhận có shiftid để nhận diện khi confirm
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
                        smtp.Port = EmailPort;
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

        public ActionResult ForgotPassword(string shiftname)
        {
            var model = db.tb_User.Where(x => x.UserName == shiftname).FirstOrDefault();
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
            string shiftID = form["UserID"];
            string passWord = form["Password"];
            string ct = form["captra"];


            if (string.IsNullOrEmpty(shiftID) ||
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
                tb_User tk = db.tb_User.Where(x => x.UserID == shiftID).FirstOrDefault();
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
                    //Nội dung mail - gửi kèm link xác nhận có shiftid để nhận diện khi confirm
                    mm.Body = $"Để xác nhận thay đổi email vui lòng nhấp vào link sau <a href=\"https://localhost:44315/Home/ConfirmEmailChange/?shiftid={Account.ID}&newEmail={newEmail}\">Xác nhận thay đổi email</a>";
                    mm.IsBodyHtml = true;
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = EmailHost;
                        smtp.EnableSsl = true;

                        //tài khoản đăng kí sử dụng smtp
                        NetworkCredential cred = new NetworkCredential(EmailFrom, EmailFromPassword);
                        smtp.UseDefaultCredentials = true;
                        smtp.Credentials = cred;
                        smtp.Port = EmailPort;
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

        public ActionResult ConfirmEmailChange(string shiftid, string newEmail)
        {
            var account = db.tb_User.Where(x => x.ID.ToString() == shiftid).FirstOrDefault();
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

        public ActionResult SearchRole(string searchString)
        {
            List<tb_Role> listRoleFill = db.tb_Role.ToList();
            List<tb_Role> listRole = new List<tb_Role>();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Unidecode().ToLower();
                foreach (var item in listRoleFill)
                {
                    var RoleName = item.RoleName.Unidecode().ToLower();
                    if (RoleName.Contains(searchString))
                    {
                        listRole.Add(item);
                    }
                }
            }
            else
            {
                listRole = db.tb_Role.ToList();
            }

            ViewBag.listRole = listRole;

            return PartialView("_TableRolePartial");
        }

        public ActionResult GetTableRole()
        {
            List<tb_Role> listRole = db.tb_Role.ToList();
            ViewBag.listRole = listRole;
            return PartialView("_TableRolePartial");
        }

        public ActionResult CreateRole()
        {
            tb_Role size = new tb_Role();
            tb_Role lastRole = db.tb_Role.OrderByDescending(x => x.ID).FirstOrDefault();
            size.RoleID = "R" + (lastRole.ID + 1);

            return PartialView("_CreateRolePartial", size);
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
            tb_Role size = db.tb_Role.FirstOrDefault(x => x.ID == ID);
            return PartialView("_EditRolePartial", size);
        }

        [HttpPost]
        public ActionResult EditRole(tb_Role model)
        {
            try
            {
                var size = db.tb_Role.FirstOrDefault(x => x.ID == model.ID);
                size.RoleID = model.RoleID;
                size.RoleName = model.RoleName;
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
                return Json(new { success = false, message = "Không thể xóa bản ghi này!!" }, JsonRequestBehavior.AllowGet);
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
        public ActionResult SearchUser(string searchString)
        {
            List<tb_User> listUserFill = db.tb_User.ToList();
            List<tb_User> listUser = new List<tb_User>();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Unidecode().ToLower();
                foreach (var item in listUserFill)
                {
                    var UserName = item.UserName.Unidecode().ToLower();
                    if (UserName.Contains(searchString))
                    {
                        listUser.Add(item);
                    }
                }
            }
            else
            {
                listUser = db.tb_User.ToList();
            }

            foreach (var item in listUser)
            {
                if (item.RoleID == null) continue;
                item.RoleName = db.tb_Role.Where(x => x.RoleID == item.RoleID).FirstOrDefault().RoleName;
            }

            ViewBag.listUser = listUser;

            return PartialView("_TableUserPartial");
        }
        public ActionResult GetTableUser()
        {
            List<tb_User> listUser = db.tb_User.ToList();

            foreach (var item in listUser)
            {
                if (item.RoleID == null) continue;
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
                return Json(new { success = false, message = "Không thể xóa bản ghi này!!" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, message = "Xóa bản ghi thành công" }, JsonRequestBehavior.AllowGet);

        }

        #endregion

        #region Các hàm xử lý Size
        public ActionResult ManageSize()
        {
            List<tb_Size> listSize = db.tb_Size.ToList();
            ViewBag.listSize = listSize;
            return View();
        }

        public ActionResult GetTableSize()
        {
            List<tb_Size> listSize = db.tb_Size.ToList();
            ViewBag.listSize = listSize;
            try
            {
                foreach (var item in listSize)
                {
                    if (item.CateID == null) continue;
                    item.CateName = db.tb_Category.Where(x => x.CateID == item.CateID).FirstOrDefault().CateName;
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi trong quá trình lấy dữ liệu!" }, JsonRequestBehavior.AllowGet);
            }
            return PartialView("_TableSizePartial");
        }

        public ActionResult SearchSize(string searchString)
        {
            List<tb_Size> listSizeFill = db.tb_Size.ToList();
            List<tb_Size> listSize = new List<tb_Size>();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Unidecode().ToLower();
                foreach (var item in listSizeFill)
                {
                    var SizeName = item.SizeName.Unidecode().ToLower();
                    if (SizeName.Contains(searchString))
                    {
                        listSize.Add(item);
                    }
                }
            }
            else
            {
                listSize = db.tb_Size.ToList();
            }

            foreach (var item in listSize)
            {
                if (item.CateID == null) continue;
                item.CateName = db.tb_Category.Where(x => x.CateID == item.CateID).FirstOrDefault().CateName;
            }

            ViewBag.listSize = listSize;

            return PartialView("_TableSizePartial");
        }

        public ActionResult CreateSize()
        {
            tb_Size size = new tb_Size();
            tb_Size lastSize = db.tb_Size.OrderByDescending(x => x.ID).FirstOrDefault();
            size.SizeID = "S" + (lastSize.ID + 1);

            var listCategory = db.tb_Category.ToList();
            ViewBag.listCategory = listCategory.ToSelectList(r => r.CateName, r => r.CateID);

            return PartialView("_CreateSizePartial", size);
        }

        [HttpPost]
        public ActionResult CreateSize(tb_Size model)
        {
            try
            {
                db.tb_Size.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Thêm mới không thành công, lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Thêm mới thành công" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult EditSize(long ID)
        {
            var listCategory = db.tb_Category.ToList();
            ViewBag.listCategory = listCategory.ToSelectList(r => r.CateName, r => r.CateID);

            tb_Size size = db.tb_Size.FirstOrDefault(x => x.ID == ID);
            return PartialView("_EditSizePartial", size);
        }

        [HttpPost]
        public ActionResult EditSize(tb_Size model)
        {
            try
            {
                var size = db.tb_Size.FirstOrDefault(x => x.ID == model.ID);
                size.SizeID = model.SizeID;
                size.SizeName = model.SizeName;
                size.CateID = model.CateID;
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Sửa bản ghi không thành công!" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Sửa bản ghi thành công!" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DeleteSize(long ID)
        {
            try
            {
                tb_Size model = db.tb_Size.Where(x => x.ID == ID).FirstOrDefault();
                db.tb_Size.Remove(model);
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Không thể xóa bản ghi này!!" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, message = "Xóa bản ghi thành công" }, JsonRequestBehavior.AllowGet);

        }

        #endregion

        #region Các hàm xử lý Shift
        public ActionResult ManageShift()
        {
            List<tb_Shift> listShift = db.tb_Shift.ToList();
            ViewBag.listShift = listShift;

            return View();
        }

        public ActionResult GetTableShift()
        {
            List<tb_Shift> listShift = db.tb_Shift.ToList();
            try
            {
                foreach (var item in listShift)
                {
                    if (item.CateID == null) continue;
                    item.CateName = db.tb_Category.Where(x => x.CateID == item.CateID).FirstOrDefault().CateName;
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi trong quá trình lấy dữ liệu!" }, JsonRequestBehavior.AllowGet);
            }

            ViewBag.listShift = listShift;

            return PartialView("_TableShiftPartial");
        }

        public ActionResult SearchShift(string searchString)
        {
            List<tb_Shift> listShiftFill = db.tb_Shift.ToList();
            List<tb_Shift> listShift = new List<tb_Shift>();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Unidecode().ToLower();
                foreach (var item in listShiftFill)
                {
                    var ShiftName = item.ShiftName.Unidecode().ToLower();
                    if (ShiftName.Contains(searchString))
                    {
                        listShift.Add(item);
                    }
                }
            }
            else
            {
                listShift = db.tb_Shift.ToList();
            }

            foreach (var item in listShift)
            {
                if (item.CateID == null) continue;
                item.CateName = db.tb_Category.Where(x => x.CateID == item.CateID).FirstOrDefault().CateName;
            }

            ViewBag.listShift = listShift;

            return PartialView("_TableShiftPartial");
        }

        public ActionResult CreateShift()
        {
            tb_Shift Shift = new tb_Shift();
            tb_Shift lastShift = db.tb_Shift.OrderByDescending(x => x.ID).FirstOrDefault();
            Shift.ShiftID = "Ca" + (lastShift.ID + 1);

            var listCategory = db.tb_Category.ToList();
            ViewBag.listCategory = listCategory.ToSelectList(r => r.CateName, r => r.CateID);

            return PartialView("_CreateShiftPartial", Shift);
        }

        [HttpPost]
        public ActionResult CreateShift(tb_Shift model)
        {
            try
            {
                db.tb_Shift.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Thêm mới không thành công, lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Thêm mới thành công" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult EditShift(long ID)
        {
            var listCategory = db.tb_Category.ToList();
            ViewBag.listCategory = listCategory.ToSelectList(r => r.CateName, r => r.CateID);

            tb_Shift Shift = db.tb_Shift.FirstOrDefault(x => x.ID == ID);
            return PartialView("_EditShiftPartial", Shift);
        }

        [HttpPost]
        public ActionResult EditShift(tb_Shift model)
        {
            try
            {
                var Shift = db.tb_Shift.FirstOrDefault(x => x.ID == model.ID);
                Shift.ShiftID = model.ShiftID;
                Shift.ShiftName = model.ShiftName;
                Shift.Price = model.Price;
                Shift.CateID = model.CateID;
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Sửa bản ghi không thành công!" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Sửa bản ghi thành công!" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DeleteShift(long ID)
        {
            try
            {
                tb_Shift model = db.tb_Shift.Where(x => x.ID == ID).FirstOrDefault();
                db.tb_Shift.Remove(model);
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Không thể xóa bản ghi này!!" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, message = "Xóa bản ghi thành công" }, JsonRequestBehavior.AllowGet);

        }

        #endregion

        #region Các hàm xử lý Arena
        public ActionResult ManageArena()
        {
            List<tb_Arena> listArena = db.tb_Arena.ToList();
            ViewBag.listArena = listArena;
            return View();
        }

        public ActionResult SearchArena(string searchString)
        {
            List<tb_Arena> listArenaFill = db.tb_Arena.ToList();
            List<tb_Arena> listArena = new List<tb_Arena>();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Unidecode().ToLower();
                foreach (var item in listArenaFill)
                {
                    var ArenaName = item.ArenaName.Unidecode().ToLower();
                    if (ArenaName.Contains(searchString))
                    {
                        listArena.Add(item);
                    }
                }
            }
            else
            {
                listArena = db.tb_Arena.ToList();
            }

            foreach (var item in listArena)
            {
                if (item.SizeID == null) continue;
                item.SizeName = db.tb_Size.Where(x => x.SizeID == item.SizeID).FirstOrDefault().SizeName;
            }

            foreach (var item in listArena)
            {
                if (item.CateID == null) continue;
                item.CateName = db.tb_Category.Where(x => x.CateID == item.CateID).FirstOrDefault().CateName;
            }

            ViewBag.listArena = listArena;

            return PartialView("_TableArenaPartial");
        }
        public ActionResult GetTableArena()
        {
            List<tb_Arena> listArena = db.tb_Arena.ToList();

            foreach (var item in listArena)
            {
                if (item.SizeID == null) continue;
                item.SizeName = db.tb_Size.Where(x => x.SizeID == item.SizeID).FirstOrDefault().SizeName;
            }

            foreach (var item in listArena)
            {
                if (item.CateID == null) continue;
                item.CateName = db.tb_Category.Where(x => x.CateID == item.CateID).FirstOrDefault().CateName;
            }

            ViewBag.listArena = listArena;

            return PartialView("_TableArenaPartial");
        }

        public ActionResult CreateArena()
        {
            tb_Arena Arena = new tb_Arena();
            tb_Arena lastArena = db.tb_Arena.OrderByDescending(x => x.ID).FirstOrDefault();
            Arena.ArenaID = "A" + (lastArena.ID + 1);

            var listCate = db.tb_Category.ToList();
            ViewBag.listCate = listCate.ToSelectList(r => r.CateName, r => r.CateID);

            return PartialView("_CreateArenaPartial", Arena);
        }

        [HttpPost]
        public ActionResult CreateArena(tb_Arena model)
        {
            try
            {

                // Xử lý file ảnh
                if (model.File != null && model.File.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(model.File.FileName);
                    string path = Path.Combine(Server.MapPath("/Uploads/UploadArena/"), fileName);

                    if (System.IO.File.Exists(path))
                    {
                        int count = 1;
                        string extension = Path.GetExtension(fileName);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        while (System.IO.File.Exists(path))
                        {
                            fileName = $"{fileNameWithoutExtension}_{count}{extension}";
                            path = Path.Combine(Server.MapPath("~/Uploads/UploadArena/"), fileName);
                            count++;
                        }
                    }

                    model.File.SaveAs(path);
                    model.Image = fileName;
                }

                db.tb_Arena.Add(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Thêm mới không thành công, lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Thêm mới thành công" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult EditArena(long ID)
        {
            var listCate = db.tb_Category.ToList();
            ViewBag.listCate = listCate.ToSelectList(r => r.CateName, r => r.CateID);

            var listSize = db.tb_Size.ToList();
            ViewBag.listSize = listSize.ToSelectList(r => r.SizeName, r => r.SizeID);

            tb_Arena Arena = db.tb_Arena.FirstOrDefault(x => x.ID == ID);
            return PartialView("_EditArenaPartial", Arena);
        }

        [HttpPost]
        public ActionResult EditArena(tb_Arena model)
        {
            try
            {
                var Arena = db.tb_Arena.FirstOrDefault(x => x.ID == model.ID);
                Arena.ArenaID = model.ArenaID;
                Arena.ArenaName = model.ArenaName;
                Arena.CateID = model.CateID;
                Arena.SizeID = model.SizeID;
                Arena.Description = model.Description;
                Arena.MaxPersons = model.MaxPersons;

                //Xử lý file ảnh
                if (model.File != null && model.File.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(model.File.FileName);
                    string path = Path.Combine(Server.MapPath("/Uploads/UploadArena/"), fileName);

                    if (System.IO.File.Exists(path))
                    {
                        int count = 1;
                        string extension = Path.GetExtension(fileName);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        while (System.IO.File.Exists(path))
                        {
                            fileName = $"{fileNameWithoutExtension}_{count}{extension}";
                            path = Path.Combine(Server.MapPath("~/Uploads/UploadArena/"), fileName);
                            count++;
                        }
                    }

                    model.File.SaveAs(path);
                    Arena.Image = fileName;
                }

                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Sửa bản ghi không thành công!" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "Sửa bản ghi thành công!" }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DeleteArena(long ID)
        {
            try
            {
                tb_Arena model = db.tb_Arena.Where(x => x.ID == ID).FirstOrDefault();
                db.tb_Arena.Remove(model);
                db.SaveChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Không thể xóa bản ghi này!!" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, message = "Xóa bản ghi thành công" }, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public ActionResult GetDropDownSise(string cateID)
        {
            var listSize = db.tb_Size.Where(x => x.CateID == cateID).ToList();
            if (listSize.Count > 0)
            {
                var Sizes = listSize.ToSelectList(r => r.SizeName, r => r.SizeID);
                return Json(new { success = true, data = Sizes }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = false });
        }

        #endregion


        public ActionResult Statistical()
        {
            return View();
        }

    }
}