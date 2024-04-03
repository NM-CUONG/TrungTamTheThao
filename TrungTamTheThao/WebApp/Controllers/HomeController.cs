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
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Login() {
            return View ();
        }

        public ActionResult SendMail()
        {
            try
            {
                using (MailMessage mm = new MailMessage("nguyenmanhcuong2k2.hsbg@gmail.com", "nguyenmanhcuong02062002@gmail.com"))
                {
                    mm.Subject = "Test Mail";
                    mm.Body = "Xin chao";
                    mm.IsBodyHtml = false;
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = "smtp.gmail.com";
                        smtp.EnableSsl = true;

                        NetworkCredential cred = new NetworkCredential("nguyenmanhcuong2k2.hsbg@gmail.com", "amyr nbnb akdc djgc");
                        smtp.UseDefaultCredentials = true;
                        smtp.Credentials = cred;
                        smtp.Port = 587;
                        smtp.Send(mm);
                        ViewBag.Message = "Da gui mail";
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            

            return View();
        }
        
    }
}