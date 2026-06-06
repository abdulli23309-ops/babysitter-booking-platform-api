using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.DTOs
{
    public class BabysitterRegistrationDTO
    {
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DOB { get; set; }
        public int ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public string PictureAddress { get; set; }
    }

    public class LoginDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

    }


    
    
}