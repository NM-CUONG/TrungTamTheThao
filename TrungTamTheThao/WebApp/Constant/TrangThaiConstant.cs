using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace WebApp.Constant
{
    public static class TrangThaiConstant
    {
        [DisplayName("Chưa xác nhận")]
        public static int ChuaXacNhan { get; set; } = 0;

        [DisplayName("Đã xác nhận")]
        public static int DaXacNhan { get; set; } = 1;


        public static List<SelectListItem> GetSelectListItems(int selectedValue = -1)
        {
            var selectListItems = new List<SelectListItem>();

            foreach (var property in typeof(TrangThaiConstant).GetProperties(BindingFlags.Public | BindingFlags.Static))
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
            foreach (var property in typeof(TrangThaiConstant).GetProperties(BindingFlags.Public | BindingFlags.Static))
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

    public static class TrangThaiBookingConstant
    {
        [DisplayName("Không thành công")]
        public static int KhongThanhCong { get; set; } = 0;

        [DisplayName("Thành công")]
        public static int ThanhCong { get; set; } = 1;

        [DisplayName("Đang sử dụng")]
        public static int DangSuDung { get; set; } = 2;

        [DisplayName("Đã sử dụng")]
        public static int DaSuDung { get; set; } = 3;

        [DisplayName("Đã hủy")]
        public static int DaHuy { get; set; } = 4;


        public static List<SelectListItem> GetSelectListItems(int selectedValue = -1)
        {
            var selectListItems = new List<SelectListItem>();

            foreach (var property in typeof(TrangThaiBookingConstant).GetProperties(BindingFlags.Public | BindingFlags.Static))
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
            foreach (var property in typeof(TrangThaiBookingConstant).GetProperties(BindingFlags.Public | BindingFlags.Static))
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