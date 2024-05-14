using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApp.Areas.VNPayAPI.Util
{
    public class VNPay
    {
        public string vnp_Amount { get; set; }
        public string vnp_BankCode { get; set; }
        public string vnp_BankTranNo { get; set; }
        public string vnp_OrderInfo { get; set; }
        public DateTime vnp_PayDate { get; set; }
        public string vnp_ResponseCode { get; set; }
        public string vnp_TransactionStatus { get; set; }
    }
}