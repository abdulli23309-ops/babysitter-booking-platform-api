using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [RoutePrefix("api/babysitter")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class BabysitterController : ApiController
    {
        // ---------------- REGISTER (multipart, stores filename only) ----------------
        [HttpPost]
        [Route("register")]
        public IHttpActionResult RegisterBabysitter()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                string fullName = httpRequest.Form["FullName"];
                string email = httpRequest.Form["EmailAddress"];
                string username = httpRequest.Form["Username"];
                string password = httpRequest.Form["Password"];
                string phone = httpRequest.Form["PhoneNumber"];
                string dobStr = httpRequest.Form["DOB"];
                string expStr = httpRequest.Form["ExperienceYears"];
                string rateStr = httpRequest.Form["HourlyRate"];
                bool useDefault = httpRequest.Form["UseDefaultPicture"] == "true";

                if (string.IsNullOrWhiteSpace(fullName) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password))
                    return BadRequest("FullName, Email and Password are required.");

                DateTime dob;
                if (!DateTime.TryParse(dobStr, out dob))
                    dob = new DateTime(2000, 1, 1);

                int exp = int.TryParse(expStr, out int e) ? e : 0;
                decimal rate = decimal.TryParse(rateStr, out decimal r) ? r : 0;

                using (var db = new BabySitterBooking_and_BabyMinderEntities())
                {
                    if (db.Babysitters.Any(b => b.EmailAddress == email || b.Username == username))
                        return BadRequest("A user with this Email or Username already exists.");

                    // Handle image
                    string fileName;
                    if (useDefault || httpRequest.Files.Count == 0)
                    {
                        fileName = "Sitters/default_sitter.jpg"; 
                    }
                    else
                    {
                        var postedFile = httpRequest.Files[0];
                        if (postedFile != null && postedFile.ContentLength > 0)
                        {
                            string ext = Path.GetExtension(postedFile.FileName).ToLower();
                            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                                return BadRequest("Only JPG/PNG images are allowed.");

                            fileName = Guid.NewGuid().ToString() + ext;
                            string folderPath = HttpContext.Current.Server.MapPath("~/Images/Sitters/");
                            if (!Directory.Exists(folderPath))
                                Directory.CreateDirectory(folderPath);

                            string fullPath = Path.Combine(folderPath, fileName);
                            postedFile.SaveAs(fullPath);
                        }
                        else
                        {
                            fileName = "default_sitter.jpg";
                        }
                    }

                    var sitter = new Babysitter
                    {
                        FullName = fullName,
                        EmailAddress = email,
                        Username = username,
                        Password = password,
                        PhoneNumber = phone,
                        DOB = dob,
                        ExperienceYears = exp,
                        HourlyRate = rate,
                        PictureAddress = "Sitters/" + fileName,
                        AvailabilityStatus = "Available",
                        CreatedAt = DateTime.Now
                    };

                    db.Babysitters.Add(sitter);
                    db.SaveChanges();

                    return Ok(new { message = "Sitter Registered Successfully" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Registration Error: " + ex.Message);
            }
        }
        [HttpGet]
        [Route("earnings/{sitterId}")]
        public IHttpActionResult GetEarnings(int sitterId)
        {
            using (var db = new BabySitterBooking_and_BabyMinderEntities())
            {
                var completedJobs = db.Jobs
                    .Where(j => j.AssignedSitter_ID == sitterId && j.Status == "Completed")
                    .ToList();

                decimal totalEarnings = completedJobs.Sum(j => j.Payment ?? 0);
                int jobCount = completedJobs.Count;

                double totalHours = 0;
                foreach (var job in completedJobs)
                {
                    var slots = db.JobTimeSlots
                        .Where(js => js.Job_ID == job.Job_ID)
                        .Join(db.TimeSlots, js => js.Slot_ID, ts => ts.Slot_ID, (js, ts) => ts)
                        .ToList();

                    foreach (var slot in slots)
                    {
                        if (slot.StartTime.HasValue && slot.EndTime.HasValue)
                            totalHours += (slot.EndTime.Value - slot.StartTime.Value).TotalHours;
                    }
                }

                var recentPayments = completedJobs
    .OrderByDescending(j => j.JobDate)
    .Take(10)
    .Select(j => new
    {
        parentName = db.Parents.Where(p => p.Parent_ID == j.Parent_ID)
                        .Select(p => p.FullName).FirstOrDefault(),
        amount = j.Payment,
        date = j.JobDate.ToString("MMM dd, yyyy • hh:mm tt")
    })
    .ToList();

                return Ok(new
                {
                    totalEarnings,
                    completedJobs = jobCount,
                    totalHours = (int)Math.Round(totalHours, 0),
                    recentPayments
                });
            }
        }

        // ---------------- LOGIN (unchanged from ParentController pattern) ----------------
        [HttpPost]
        [Route("login")]
        public IHttpActionResult LoginBabysitter(WebApplication2.DTOs.LoginDTO login)
        {
            if (login == null)
                return BadRequest("Login details missing");

            try
            {
                // Ensure the role sent is "Sitter"
                if (login.Role != "Sitter")
                    return BadRequest("Invalid role for this endpoint");

                using (var db = new BabySitterBooking_and_BabyMinderEntities())
                {
                    var sitter = db.Babysitters.FirstOrDefault(s => s.Username == login.Username);

                    if (sitter == null)
                        return Content(HttpStatusCode.Unauthorized, "User not found");

                    if (sitter.Password != login.Password)
                        return Content(HttpStatusCode.Unauthorized, "Wrong password");

                    return Ok(new
                    {
                        message = "Login Successful",
                        userId = sitter.Sitter_ID,
                        name = sitter.FullName,
                        role = "Sitter",
                        // you can add more fields if needed (e.g., hourly rate)
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Login Error: " + ex.Message);
            }
        }
    }
}