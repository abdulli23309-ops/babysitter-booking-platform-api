using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.DTOs
{
    public class ReviewDTO
    {
        public int Job_ID { get; set; }

        public int Reviewer_ID { get; set; }
        public string ReviewerRole { get; set; }

        public int ReviewFor_ID { get; set; }
        public string ReviewForRole { get; set; }

        public decimal Rating { get; set; }
        public string Comment { get; set; }
    }
}