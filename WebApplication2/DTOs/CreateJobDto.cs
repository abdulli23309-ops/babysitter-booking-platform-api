using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.DTOs
{
    public class CreateJobDto
    {
        public int ParentId { get; set; }
        public int SitterId { get; set; }
        public int ChildId { get; set; }
        public string City { get; set; }
        public DateTime StartDate { get; set; }   // the job date (same as search start date for one-day)
        public string StartTime { get; set; }     // "HH:mm"
        public string EndTime { get; set; }
    }
}