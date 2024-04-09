using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApp.Models
{
    public class BookTimeEmpty
    {
        public int startTime { get; set; }
        public int endTime { get; set; }

        public BookTimeEmpty(int startTime, int endTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;
        }

        public BookTimeEmpty()
        {
        }
    }

    
}