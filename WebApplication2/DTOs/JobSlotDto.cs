using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.DTOs
{
    public class JobSlotDto
    {

        public int JobId { get; set; }
        public List<int> SlotIds { get; set; }
    }
}