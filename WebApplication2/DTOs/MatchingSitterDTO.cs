using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.DTOs
{
    public class MatchingSitterDTO
    {
        public int Sitter_ID { get; set; }
        public string FullName { get; set; }
        public decimal HourlyRate { get; set; }
    }
}
