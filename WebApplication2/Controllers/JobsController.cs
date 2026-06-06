using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApplication2.DTOs;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [RoutePrefix("api/jobs")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class JobsController : ApiController
    {
        private BabySitterBooking_and_BabyMinderEntities db = new BabySitterBooking_and_BabyMinderEntities();

        // ── GET /api/jobs?city=Islamabad ──────────────────────────────────────
        // Returns only unassigned (open) jobs, each with their full SlotIds array.
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetJobs(string city = null)
        {
            try
            {
                var query = db.Jobs.Where(j => j.AssignedSitter_ID == null && j.Status == "Open");

                if (!string.IsNullOrEmpty(city))
                {
                    string cityLower = city.Trim().ToLower();
                    query = query.Where(j => j.City.ToLower().Trim() == cityLower);
                }

                // We need to materialise first before doing the SlotIds sub-select
                var jobList = query.ToList();

                var result = jobList.Select(j => new
                {
                    j.Job_ID,
                    j.Title,
                    j.Status,
                    j.JobDate,
                    j.City,
                    j.Payment,
                    ParentAddress = j.Parent != null ? j.Parent.Address : null,
                    ParentPic = j.Parent != null ? j.Parent.PictureAddress : null,
                    ParentName = j.Parent != null ? j.Parent.FullName : null,

                    // ✅ Key fix: return ALL slot IDs for this job as an array
                    SlotIds = db.JobTimeSlots
                        .Where(js => js.Job_ID == j.Job_ID)
                        .Select(js => js.Slot_ID)
                        .ToList(),

                    // Parent rating from polymorphic Review table
                    Rating = db.Reviews
                        .Where(r => r.ReviewFor_ID == j.Parent_ID && r.ReviewForRole == "Parent")
                        .Average(r => (decimal?)r.Rating) ?? 0
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ── GET /api/jobs/jobdetails/{jobId} ──────────────────────────────────
        [HttpGet]
        [Route("jobdetails/{jobId}")]
        public IHttpActionResult GetJobDetails(int jobId)
        {
            try
            {
                var j = db.Jobs.FirstOrDefault(x => x.Job_ID == jobId);
                if (j == null) return NotFound();

                var slotIds = db.JobTimeSlots
                    .Where(js => js.Job_ID == jobId)
                    .Select(js => js.Slot_ID)
                    .ToList();

                var parentRating = db.Reviews
                    .Where(r => r.ReviewFor_ID == j.Parent_ID && r.ReviewForRole == "Parent")
                    .Average(r => (decimal?)r.Rating) ?? 0;

                int childAge = 0;
                if (j.Child?.DOB != null)
                {
                    childAge = DateTime.Now.Year - j.Child.DOB.Year;
                    if (DateTime.Now < j.Child.DOB.AddYears(childAge)) childAge--;
                }

                return Ok(new
                {
                    j.Job_ID,
                    j.Title,
                    j.Description,
                    j.JobDate,
                    j.Status,
                    j.City,
                    j.Payment,
                    SlotIds = slotIds,
                    ParentName = j.Parent?.FullName,
                    ParentAddress = j.Parent?.Address,
                    ParentPic = j.Parent?.PictureAddress,
                    ParentRating = parentRating,
                    ChildName = j.Child?.ChildName,
                    ChildAge = childAge,
                    Gender = j.Child?.Gender,
                    PictureAddress = j.Child?.PictureAddress,
                    j.AssignedSitter_ID
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("confirm-bulk")]
        public IHttpActionResult ConfirmJobsBulk([FromBody] BulkConfirmDto dto)
        {
            if (dto?.JobIds == null || dto.JobIds.Count == 0)
                return BadRequest("No jobs provided.");

            var jobs = db.Jobs.Where(j => dto.JobIds.Contains(j.Job_ID) && j.Status == "Open").ToList();
            if (jobs.Count != dto.JobIds.Count)
                return BadRequest("One or more jobs are no longer available.");

            foreach (var job in jobs)
            {
                job.AssignedSitter_ID = dto.SitterId;
                job.Status = "Assigned";
            }

            db.SaveChanges();
            return Ok(new { message = $"{jobs.Count} jobs confirmed." });
        }

        // ── POST /api/jobs/confirm/{jobId}/{sitterId} ─────────────────────────
        // Called when a sitter accepts a job
        [HttpPost]
        [Route("confirm/{jobId}/{sitterId}")]
        public IHttpActionResult ConfirmJob(int jobId, int sitterId)
        {
            try
            {
                var job = db.Jobs.FirstOrDefault(j => j.Job_ID == jobId);
                if (job == null) return NotFound();

                if (job.AssignedSitter_ID != null)
                    return BadRequest("Job already assigned.");

                job.AssignedSitter_ID = sitterId;
                job.Status = "Assigned";
                db.SaveChanges();

                return Ok(new { message = "Job confirmed." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("updateStatus/{jobId}")]
        public IHttpActionResult UpdateJobStatus(int jobId, [FromBody] JobStatusUpdateDto dto)
        {
            var job = db.Jobs.FirstOrDefault(j => j.Job_ID == jobId);
            if (job == null) return NotFound();

            job.Status = dto.Status;   // e.g., "In Progress", "Completed"
            db.SaveChanges();
            return Ok(new { message = "Job status updated." });
        }
        [HttpGet]
        [Route("sitter/{sitterId}")]
        public IHttpActionResult GetSitterJobs(int sitterId)
        {
            var jobs = db.Jobs
                .Where(j => j.AssignedSitter_ID == sitterId)
                .Select(j => new {
                    j.Job_ID,
                    j.Title,
                    j.JobDate,
                    j.Status,
                    j.Payment,
                    j.City,
                    ChildName = j.Child.ChildName,
                    ChildAge = DbFunctions.DiffYears(j.Child.DOB, DateTime.Now) ?? 0,
                    ParentName = j.Parent.FullName,
                    ParentPhone = j.Parent.PhoneNumber,
                    ParentAddress = j.Parent.Address,
                    ParentPic = j.Parent.PictureAddress,
                    ParentRating = db.Reviews
                        .Where(r => r.ReviewFor_ID == j.Parent_ID && r.ReviewForRole == "Parent")
                        .Average(r => (decimal?)r.Rating) ?? 0,   // <-- add this
                    SlotTimes = db.JobTimeSlots
                        .Where(js => js.Job_ID == j.Job_ID)
                        .Select(js => new { StartTime = js.TimeSlot.StartTime, EndTime = js.TimeSlot.EndTime })
                        .ToList()
                })
                .ToList();

            return Ok(jobs);
        }
        // GET api/jobs/active?babysitterId=19
        // GET api/jobs/active?babysitterId=20
        [HttpGet]
        [Route("active")]
        public IHttpActionResult GetActiveJobForSitter(int babysitterId)
        {
            try
            {
                var today = DateTime.Today;
                var job = db.Jobs
                    .Where(j => j.AssignedSitter_ID == babysitterId
                             && (j.Status == "Assigned" || j.Status == "In Progress")
                             && DbFunctions.TruncateTime(j.JobDate) == today)
                    .OrderByDescending(j => j.Job_ID)
                    .FirstOrDefault();

                if (job == null)
                    return NotFound();

                // Use navigation properties – no extra queries needed
                return Ok(new
                {
                    jobId = job.Job_ID,
                    parentId = job.Parent_ID,
                    childId = job.Child_ID,
                    parentName = job.Parent?.FullName,
                    childName = job.Child?.ChildName
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

}