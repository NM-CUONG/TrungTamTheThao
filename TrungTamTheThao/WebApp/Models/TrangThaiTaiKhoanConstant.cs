using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.WebPages.Html;

namespace WebApp.Models
{
    public class TrangThaiTaiKhoanConstant
    {
        [DisplayName("Chưa xác nhận")]
        public static int ChuaXacNhan { get; set; } = 0;

        [DisplayName("Đã xác nhận")]
        public static int DaXacNhan { get; set; } = 1;

        public static string GetDisplayNameByValue(int value)
        {
            Type type = typeof(tb_User);
            PropertyInfo property = type.GetProperty("Status");

            if (property != null && property.PropertyType == typeof(int))
            {
                var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();
                if (displayNameAttribute != null)
                {
                    return displayNameAttribute.DisplayName;
                }
            }

            return null;
        }

        public static List<SelectListItem> GetSelectListItems()
        {
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            Type type = typeof(TrangThaiTaiKhoanConstant);
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == typeof(int))
                {
                    var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();
                    if (displayNameAttribute != null)
                    {
                        int value = (int)property.GetValue(null);
                        string displayName = displayNameAttribute.DisplayName;
                        selectListItems.Add(new SelectListItem { Value = value.ToString(), Text = displayName });
                    }
                }
            }

            return selectListItems;
        }
    }
}