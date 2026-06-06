using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.DTOs
{
    public class AvailabilityDto
    {
        public int SitterId { get; set; }
        public DateTime? Date { get; set; }
        public List<int> SlotIds { get; set; }
        public string City { get; set; }
    }
}