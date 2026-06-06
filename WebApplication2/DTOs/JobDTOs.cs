using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.DTOs
{
    public class SitterDTO
    {
        public int Sitter_ID { get; set; }
        public string FullName { get; set; }
        public decimal HourlyRate { get; set; }
        public int ExperienceYears { get; set; }
        public string PictureAddress { get; set; }
        public decimal Rating { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DOB { get; set; }
    }

    public class BidDTO
    {
        public int Bid_ID { get; set; }
        public decimal ProposedPrice { get; set; }
        public string BidStatus { get; set; }
        public SitterDTO Babysitter { get; set; }
    }

    public class JobDTO
    {
        public int Job_ID { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public DateTime? JobDate { get; set; }
        public List<BidDTO> Bids { get; set; }
    }
    public class JobStatusUpdateDto
    {
        public string Status { get; set; }
    }
}