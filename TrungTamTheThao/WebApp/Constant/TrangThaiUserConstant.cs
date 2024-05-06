using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace WebApp.Constant
{
    public static class TrangThaiUserConstant
    {
        [DisplayName("Chưa xác nhận")]
        public static int ChuaXacNhan { get; set; } = 0;

        [DisplayName("Đã xác nhận")]
        public static int DaXacNhan { get; set; } = 1;


        public static List<SelectListItem> GetSelectListItems(int selectedValue = -1)
        {
            var selectListItems = new List<SelectListItem>();

            foreach (var property in typeof(TrangThaiUserConstant).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var displayName = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                           .FirstOrDefault() as DisplayNameAttribute;
                var value = property.GetValue(null).ToString();

                var selectListItem = new SelectListItem
                {
                    Text = displayName?.DisplayName ?? property.Name,
                    Value = value,
                    Selected = value == selectedValue.ToString()
                };

                selectListItems.Add(selectListItem);
            }

            return selectListItems;
        }

        public static string GetDisplayName(int value)
        {
            foreach (var property in typeof(TrangThaiUserConstant).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var displayName = property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                           .FirstOrDefault() as DisplayNameAttribute;
                var propValue = (int)property.GetValue(null);

                if (propValue == value)
                {
                    return displayName?.DisplayName ?? property.Name;
                }
            }

            return null; 
        }

    }


}