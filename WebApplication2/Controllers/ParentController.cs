using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApplication2.DTOs;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [RoutePrefix("api/parent")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ParentController : ApiController
    {
        // ---------------- REGISTER (NOW MULTIPART) ----------------
        [HttpPost]
        [Route("register")]
        public IHttpActionResult RegisterParent()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // Read form fields
                string fullName = httpRequest.Form["FullName"];
                string email = httpRequest.Form["EmailAddress"];
                string username = httpRequest.Form["Username"];
                string password = httpRequest.Form["Password"];
                string phone = httpRequest.Form["PhoneNumber"];
                string address = httpRequest.Form["Address"] ?? "Not Provided";
                bool useDefault = httpRequest.Form["UseDefaultPicture"] == "true";

                if (string.IsNullOrWhiteSpace(fullName) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password))
                    return BadRequest("FullName, Email and Password are required.");

                // Use a fresh context to avoid concurrency exceptions
                using (var db = new BabySitterBooking_and_BabyMinderEntities())
                {
                    if (db.Parents.Any(p => p.EmailAddress == email || p.Username == username))
                        return BadRequest("A user with this Email or Username already exists.");

                    // Handle image file – save to server, store filename only
                    string fileName = null;
                    if (useDefault || httpRequest.Files.Count == 0)
                    {
                        fileName = "default_parent.jpg"; // make sure this file exists in Images/Parents/
                    }
                    else
                    {
                        var postedFile = httpRequest.Files[0];
                        if (postedFile != null && postedFile.ContentLength > 0)
                        {
                            // Validate extension
                            string ext = Path.GetExtension(postedFile.FileName).ToLower();
                            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                                return BadRequest("Only JPG/PNG images are allowed.");

                            // Generate a unique filename to avoid collisions
                            fileName = Guid.NewGuid().ToString() + ext;

                            // Ensure the directory exists
                            string folderPath = HttpContext.Current.Server.MapPath("~/Images/Parents/");
                            if (!Directory.Exists(folderPath))
                                Directory.CreateDirectory(folderPath);

                            // Save the file
                            string fullPath = Path.Combine(folderPath, fileName);
                            postedFile.SaveAs(fullPath);
                        }
                        else
                        {
                            fileName = "default_parent.jpg";
                        }
                    }

                    var newParent = new Parent
                    {
                        FullName = fullName,
                        EmailAddress = email,
                        Username = username,
                        Password = password,
                        PhoneNumber = phone,
                        Address = address,
                        PictureAddress = "Parents/" + fileName, // filename only, no base64
                        CreatedAt = DateTime.Now     // fix NULL issue
                    };

                    db.Parents.Add(newParent);
                    db.SaveChanges();

                    return Ok(new { message = "Parent Registered Successfully" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Registration Error: " + ex.Message);
            }
        }

        // ---------------- LOGIN (unchanged – uses JSON) ----------------
        [HttpPost]
        [Route("login")]
        public IHttpActionResult LoginParent(WebApplication2.DTOs.LoginDTO login)
        {
            if (login == null)
                return BadRequest("Login details missing");

            try
            {
                if (login.Role != "Parent")
                    return BadRequest("Invalid role for this endpoint");

                using (var db = new BabySitterBooking_and_BabyMinderEntities())
                {
                    var parent = db.Parents.FirstOrDefault(x => x.Username == login.Username);

                    if (parent == null)
                        return Content(HttpStatusCode.Unauthorized, "User not found");

                    if (parent.Password != login.Password)
                        return Content(HttpStatusCode.Unauthorized, "Wrong password");

                    return Ok(new
                    {
                        message = "Login Successful",
                        userId = parent.Parent_ID,
                        name = parent.FullName,
                        role = "Parent",
                        address = parent.Address,
                        pictureAddress = parent.PictureAddress
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Login Error: " + ex.Message);
            }
        }

        // ---------------- GET CHILDREN (unchanged) ----------------
        [HttpGet]
        [Route("children/{parentId}")]
        public IHttpActionResult GetChildren(int parentId)
        {
            using (var db = new BabySitterBooking_and_BabyMinderEntities())
            {
                var children = db.Children
                    .Where(c => c.Parent_ID == parentId)
                    .Select(c => new
                    {
                        c.Child_ID,
                        c.Parent_ID,
                        c.ChildName,
                        c.DOB,
                        c.Gender,
                        c.PictureAddress,
                        c.SpecialRequirements
                    })
                    .ToList();
                return Ok(children);
            }
        }
        [HttpPost]
        [Route("child")]
        public IHttpActionResult CreateChild()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // Read form fields
                int parentId = int.Parse(httpRequest.Form["ParentId"]);
                string childName = httpRequest.Form["ChildName"];
                string dobStr = httpRequest.Form["DOB"];
                string gender = httpRequest.Form["Gender"] ?? "Male";
                string special = httpRequest.Form["SpecialRequirements"];
                string guardianName = httpRequest.Form["GuardianName"];
                string guardianRelation = httpRequest.Form["GuardianRelation"];
                string guardianContact = httpRequest.Form["GuardianContact"];
                bool useDefault = httpRequest.Form["UseDefaultPicture"] == "true";

                if (string.IsNullOrWhiteSpace(childName))
                    return BadRequest("Child name is required.");

                DateTime dob;
                if (!DateTime.TryParse(dobStr, out dob))
                    dob = new DateTime(2023, 1, 1);

                // Handle image
                string fileName;
                if (useDefault || httpRequest.Files.Count == 0)
                {
                    fileName = "default_child.jpg";
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
                        string folderPath = HttpContext.Current.Server.MapPath("~/Images/Children/");
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        string fullPath = Path.Combine(folderPath, fileName);
                        postedFile.SaveAs(fullPath);
                    }
                    else
                    {
                        fileName = "default_child.jpg";
                    }
                }

                using (var db = new BabySitterBooking_and_BabyMinderEntities())
                {
                    var child = new Child
                    {
                        Parent_ID = parentId,
                        ChildName = childName,
                        DOB = dob,
                        Gender = gender,
                        SpecialRequirements = special,
                        PictureAddress = "Children/" + fileName,
                        GuardianName = guardianName,
                        GuardianRelation = guardianRelation,
                        GuardianContact = guardianContact
                    };

                    db.Children.Add(child);
                    db.SaveChanges();

                    return Ok(new { message = "Child added successfully", childId = child.Child_ID });
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }
        [HttpPost]
        [Route("create-job")]
        public IHttpActionResult CreateJobForSitter([FromBody] CreateJobDto dto)
        {
            if (dto == null || dto.SitterId <= 0 || dto.ChildId <= 0)
                return BadRequest("Invalid job data.");

            using (var db = new BabySitterBooking_and_BabyMinderEntities())
            {
                // 1. Get sitter's hourly rate
                var sitter = db.Babysitters.FirstOrDefault(s => s.Sitter_ID == dto.SitterId);
                if (sitter == null) return BadRequest("Sitter not found.");

                decimal hourlyRate = sitter.HourlyRate ?? 0;

                // 2. Calculate total hours based on start & end time
                TimeSpan startTime, endTime;
                if (!TimeSpan.TryParse(dto.StartTime, out startTime) || !TimeSpan.TryParse(dto.EndTime, out endTime))
                    return BadRequest("Invalid time format.");
                if (endTime <= startTime)
                    return BadRequest("End time must be after start time.");

                double totalHours = (endTime - startTime).TotalHours;
                decimal payment = hourlyRate * (decimal)totalHours;

                // 3. Create the job
                var job = new Job
                {
                    Parent_ID = dto.ParentId,
                    Child_ID = dto.ChildId,
                    Title = $"Care needed in {dto.City ?? "your city"} on {dto.StartDate:MMM dd}",
                    Description = "Babysitting required",
                    JobDate = dto.StartDate,          // main job date
                    Status = "Open",
                    AssignedSitter_ID = null,
                    Payment = payment,
                    City = dto.City ?? ""
                };
                db.Jobs.Add(job);
                db.SaveChanges();   // to get Job_ID

                // 4. Determine which slot IDs cover the requested time range
                var allSlots = db.TimeSlots.ToList();
                var requiredSlotIds = allSlots
                    .Where(ts => ts.StartTime.HasValue && ts.EndTime.HasValue &&
                                 ts.StartTime.Value < endTime && ts.EndTime.Value > startTime)
                    .Select(ts => ts.Slot_ID)
                    .ToList();

                if (!requiredSlotIds.Any())
                {
                    // Rollback: delete the job we just created
                    db.Jobs.Remove(job);
                    db.SaveChanges();
                    return BadRequest("No suitable time slots found for the given time range.");
                }

                // 5. Create JobTimeSlot entries
                foreach (var slotId in requiredSlotIds)
                {
                    db.JobTimeSlots.Add(new JobTimeSlot
                    {
                        Job_ID = job.Job_ID,
                        Slot_ID = slotId
                    });
                }

                db.SaveChanges();

                return Ok(new
                {
                    message = "Job created successfully. The sitter can now accept it.",
                    jobId = job.Job_ID
                });
            }
        }
        [HttpGet]
        [Route("jobs/{parentId}")]
        public IHttpActionResult GetParentJobs(int parentId)
        {
            using (var db = new BabySitterBooking_and_BabyMinderEntities())
            {
                var jobs = db.Jobs
     .Where(j => j.Parent_ID == parentId)
     .Select(j => new
     {
         j.Job_ID,
         j.Title,
         j.JobDate,
         j.Status,
         j.City,
         j.Payment,
         j.AssignedSitter_ID,          // ← ADD THIS LINE
         ChildName = j.Child.ChildName,
         SitterName = j.Babysitter.FullName,      // adjust navigation property if needed
         SitterPhone = j.Babysitter.PhoneNumber,
         SitterPicture = j.Babysitter.PictureAddress,
         SlotTimes = db.JobTimeSlots
             .Where(js => js.Job_ID == j.Job_ID)
             .Select(js => new {
                 StartTime = js.TimeSlot.StartTime,
                 EndTime = js.TimeSlot.EndTime
             }).ToList()
     }).ToList();

                return Ok(jobs);
            }
        }
    }
}