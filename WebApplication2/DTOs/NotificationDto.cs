using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string UserRole { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; }
    }
}