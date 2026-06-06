using System;
using System.Collections.Generic;
using System.Data.Common.CommandTrees.ExpressionBuilder;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApplication2.DTOs;
using System.Data.Entity;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [RoutePrefix("api/matching")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class MatchingController : ApiController
    {
        private BabySitterBooking_and_BabyMinderEntities db = new BabySitterBooking_and_BabyMinderEntities();

        // ── GET matching sitters for a job ────────────────────────────────────
        [HttpGet]
        [Route("matches/{jobId}")]
        public IHttpActionResult GetMatchingSitters(int jobId)
        {
            try
            {
                var job = db.Jobs.FirstOrDefault(j => j.Job_ID == jobId);
                if (job == null) return NotFound();

                string jobCity = (job.City ?? "").Trim().ToLower();

                // Get all slot IDs for this job
                var jobSlotIds = db.JobTimeSlots
                    .Where(js => js.Job_ID == jobId)
                    .Select(js => js.Slot_ID)
                    .ToList();

                var result = (
                    from b in db.Babysitters
                    join sa in db.SitterAvailabilities on b.Sitter_ID equals sa.Sitter_ID
                    where sa.AvailableDate == job.JobDate
                       && jobSlotIds.Contains(sa.Slot_ID)
                       && sa.City.Trim().ToLower() == jobCity
                    select new
                    {
                        b.Sitter_ID,
                        b.FullName,
                        b.HourlyRate,
                        b.ExperienceYears,
                        b.PictureAddress,
                        Rating = db.Reviews
                            .Where(r => r.ReviewFor_ID == b.Sitter_ID && r.ReviewForRole == "Sitter")
                            .Average(r => (decimal?)r.Rating) ?? 0
                    }
                ).Distinct().ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }

        // ── POST save sitter availability ──────────────────────────────────────
        // Body: { sitterId, date, slotIds: [1,2,3], city }
        [HttpPost]
        [Route("availability/save")]
        public IHttpActionResult SaveAvailability([FromBody] AvailabilityDto dto)
        {
            try
            {
                if (dto == null || dto.SlotIds == null || dto.SlotIds.Count == 0)
                    return BadRequest("Invalid payload.");

                // Remove existing availability for this sitter on this date
                // so re-saving always reflects the latest grid
                var existing = db.SitterAvailabilities
                    .Where(sa => sa.Sitter_ID == dto.SitterId && DbFunctions.TruncateTime(sa.AvailableDate) == DbFunctions.TruncateTime(dto.Date))
                    .ToList();
                foreach (var item in existing)
                {
                    db.SitterAvailabilities.Remove(item);
                }

                // Insert fresh rows
                foreach (var slotId in dto.SlotIds)
                {
                    db.SitterAvailabilities.Add(new SitterAvailability
                    {
                        Sitter_ID = dto.SitterId,
                        AvailableDate = dto.Date,
                        Slot_ID = slotId,
                        City = (dto.City ?? "").Trim()
                    });
                }

                db.SaveChanges();
                return Ok(new { message = "Availability saved." });
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                {
                    msg += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                        msg += " | Inner2: " + ex.InnerException.InnerException.Message;
                }
                return BadRequest("Error: " + msg);
            }
        }

        // ── POST filter sitters by city + experience ───────────────────────────
        [HttpPost]
        [Route("filter-sitters")]
        public IHttpActionResult FilterSitters([FromBody] FilterSittersDTO filter)
        {
            try
            {
                var query = db.Babysitters.AsQueryable();

                if (filter.MinExperience.HasValue)
                    query = query.Where(s => s.ExperienceYears >= filter.MinExperience);

                if (!string.IsNullOrEmpty(filter.City))
                {
                    string city = filter.City.ToLower().Trim();
                    query = query.Where(s => db.SitterAvailabilities
                        .Any(sa => sa.Sitter_ID == s.Sitter_ID &&
                                   sa.City.ToLower().Trim() == city));
                }

                var sitters = query.ToList().Select(s => new SitterDTO
                {
                    Sitter_ID = s.Sitter_ID,
                    FullName = s.FullName,
                    HourlyRate = s.HourlyRate ?? 0,
                    ExperienceYears = s.ExperienceYears ?? 0,
                    PictureAddress = s.PictureAddress,
                    Rating = db.Reviews
                        .Where(r => r.ReviewFor_ID == s.Sitter_ID && r.ReviewForRole == "Sitter")
                        .Average(r => (decimal?)r.Rating) ?? 0
                }).ToList();

                if (filter.MinRating.HasValue)
                    sitters = sitters.Where(s => s.Rating >= filter.MinRating).ToList();

                return Ok(sitters);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("availability/{sitterId}")]
        public IHttpActionResult GetSitterAvailability(int sitterId)
        {
            try
            {
                var rows = db.SitterAvailabilities
                    .Where(sa => sa.Sitter_ID == sitterId)
                    .Select(sa => new
                    {
                        sa.Availability_ID,
                        sa.Sitter_ID,
                        AvailableDate = sa.AvailableDate,   // returned as ISO string by JSON serialiser
                        sa.Slot_ID,
                        sa.City
                    })
                    .ToList();

                return Ok(rows);
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }
        [HttpDelete]
        [Route("availability/clear/{sitterId}")]
        public IHttpActionResult ClearAllAvailability(int sitterId)
        {
            try
            {
                var toDelete = db.SitterAvailabilities.Where(sa => sa.Sitter_ID == sitterId);
                db.SitterAvailabilities.RemoveRange(toDelete);
                db.SaveChanges();
                return Ok(new { message = "All availability cleared." });
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }
        [HttpPost]
        [Route("search-sitters")]
        public IHttpActionResult SearchSitters([FromBody] SearchSittersDTO dto)
        {
            try
            {
                // --- Validate inputs ---
                if (string.IsNullOrWhiteSpace(dto.StartTime) || string.IsNullOrWhiteSpace(dto.EndTime))
                    return BadRequest("StartTime and EndTime are required.");

                TimeSpan startTimeParsed, endTimeParsed;
                if (!TimeSpan.TryParse(dto.StartTime.Trim(), out startTimeParsed))
                    return BadRequest($"Invalid StartTime format: '{dto.StartTime}'. Expected HH:mm.");
                if (!TimeSpan.TryParse(dto.EndTime.Trim(), out endTimeParsed))
                    return BadRequest($"Invalid EndTime format: '{dto.EndTime}'. Expected HH:mm.");

                if (startTimeParsed >= endTimeParsed)
                    return BadRequest("StartTime must be before EndTime.");

                // --- 1. Base query – sitters in the city with min experience & rating ---
                var query = db.Babysitters.AsQueryable();

                if (dto.MinExperienceYears.HasValue)
                    query = query.Where(s => s.ExperienceYears >= dto.MinExperienceYears);

                if (!string.IsNullOrEmpty(dto.City))
                {
                    string cityLower = dto.City.ToLower().Trim();
                    query = query.Where(s => db.SitterAvailabilities
                        .Any(sa => sa.Sitter_ID == s.Sitter_ID &&
                                   sa.City.ToLower().Trim() == cityLower));
                }

                var sitters = query.ToList().Select(s => new SitterDTO
                {
                    Sitter_ID = s.Sitter_ID,
                    FullName = s.FullName,
                    HourlyRate = s.HourlyRate ?? 0,
                    ExperienceYears = s.ExperienceYears ?? 0,
                    PictureAddress = s.PictureAddress,
                    Rating = db.Reviews
                        .Where(r => r.ReviewFor_ID == s.Sitter_ID && r.ReviewForRole == "Sitter")
                        .Average(r => (decimal?)r.Rating) ?? 0
                }).Where(s => s.Rating >= dto.MinRating).ToList();

                if (!sitters.Any())
                    return Ok(new List<SitterDTO>());

                // --- 2. Get all time slots and filter those that intersect the requested time window ---
                var allTimeSlots = db.TimeSlots.ToList(); // assume TimeSlots table exists
                var requiredSlotIds = allTimeSlots
                    .Where(ts => ts.StartTime.HasValue && ts.EndTime.HasValue &&
                                 ts.StartTime.Value < endTimeParsed && ts.EndTime.Value > startTimeParsed)
                    .Select(ts => ts.Slot_ID)
                    .ToList();

                if (!requiredSlotIds.Any())
                    return Ok(new List<SitterDTO>());

                // --- 3. Generate dates based on selected days and date range ---
                if (!DateTime.TryParse(dto.StartDate, out DateTime startDate))
                    return BadRequest($"Invalid StartDate: '{dto.StartDate}'.");
                if (!DateTime.TryParse(dto.EndDate, out DateTime endDate))
                    return BadRequest($"Invalid EndDate: '{dto.EndDate}'.");

                if (startDate > endDate)
                    return BadRequest("StartDate must be before or equal to EndDate.");

                var datesToCheck = new List<DateTime>();

                if (dto.AvailabilityType == "One Day")
                {
                    // Always use the start date for One Day, regardless of selectedDays
                    datesToCheck.Add(startDate);
                }
                else
                {
                    // For Repeat Days, only include dates whose day name is in selectedDays
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        if (dto.SelectedDays != null && dto.SelectedDays.Contains(date.DayOfWeek.ToString()))
                            datesToCheck.Add(date);
                    }
                }

                if (!datesToCheck.Any())
                    return Ok(new List<SitterDTO>());
                // --- 4. Filter sitters who have availability for ALL required slots on ALL dates ---
                // --- 4. Filter sitters who have availability for ALL required slots on ALL dates AND no conflicting jobs ---
                var finalSitters = sitters.Where(s =>
                {
                    foreach (var date in datesToCheck)
                    {
                        // a) Availability check
                        var availableSlotIds = db.SitterAvailabilities
                            .Where(sa => sa.Sitter_ID == s.Sitter_ID &&
                                         DbFunctions.TruncateTime(sa.AvailableDate) == date.Date &&
                                         sa.City.ToLower().Trim() == dto.City.ToLower().Trim())
                            .Select(sa => sa.Slot_ID)
                            .ToList();

                        if (!requiredSlotIds.All(id => availableSlotIds.Contains(id)))
                            return false;

                        // b) Conflict check (commute buffer included)
                        if (HasConflictingJobs(s.Sitter_ID, date, requiredSlotIds, requireCommuteBuffer: true))
                            return false;
                    }
                    return true;
                }).ToList();

                return Ok(finalSitters);
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }
        // GET api/matching/jobrequests?sitterId=20
        [HttpGet]
        [Route("jobrequests")]
        public IHttpActionResult GetJobRequestsForSitter(int sitterId)
        {
            try
            {
                // 1. Get the sitter’s availability (date + slots + city)
                var availability = db.SitterAvailabilities
                    .Where(sa => sa.Sitter_ID == sitterId)
                    .Select(sa => new { sa.AvailableDate, sa.Slot_ID, sa.City })
                    .ToList();

                // 2. Build a list of (date, city, slots)
                var availabilityGroups = availability
                    .GroupBy(a => new { a.AvailableDate, a.City })
                    .Select(g => new
                    {
                        Date = g.Key.AvailableDate,
                        City = g.Key.City,
                        SlotIds = g.Select(x => x.Slot_ID).ToList()
                    })
                    .ToList();

                // 3. Find open jobs that match any of these availability entries
                var matchingJobs = new List<object>();

                foreach (var avail in availabilityGroups)
                {
                    // Get jobs on that date, in that city, that are open
                    var jobsOnDate = db.Jobs
                        .Where(j => j.Status == "Open"
                                 && DbFunctions.TruncateTime(j.JobDate) == DbFunctions.TruncateTime(avail.Date)
                                 && j.City.Trim().ToLower() == avail.City.Trim().ToLower())
                        .ToList();

                    foreach (var job in jobsOnDate)
                    {
                        // Get job’s required slots
                        var jobSlotIds = db.JobTimeSlots
                            .Where(js => js.Job_ID == job.Job_ID)
                            .Select(js => js.Slot_ID)
                            .ToList();

                        // Match if all required slots are in the sitter’s available slots for that date
                        bool allSlotsAvailable = jobSlotIds.All(slot => avail.SlotIds.Contains(slot));
                        if (allSlotsAvailable)
                        {
                            matchingJobs.Add(new
                            {
                                job.Job_ID,
                                job.Title,
                                job.Description,
                                job.JobDate,
                                job.City,
                                job.Payment,
                                job.Status,
                                RequiredSlotIds = jobSlotIds,
                                ParentName = job.Parent.FullName,
                                ChildName = job.Child.ChildName
                            });
                        }
                    }
                }

                // Remove duplicates (same job could match multiple availability records)
                var result = matchingJobs.GroupBy(j => ((dynamic)j).Job_ID).Select(g => g.First()).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }
        [HttpGet]
        [Route("babysitter/{id}")]
        public IHttpActionResult GetBabysitterDetails(int id)
        {
            var s = db.Babysitters.FirstOrDefault(b => b.Sitter_ID == id);
            if (s == null) return NotFound();

            var dto = new SitterDTO
            {
                Sitter_ID = s.Sitter_ID,
                FullName = s.FullName,
                HourlyRate = s.HourlyRate ?? 0,
                ExperienceYears = s.ExperienceYears ?? 0,
                PictureAddress = s.PictureAddress,
                EmailAddress = s.EmailAddress,
                PhoneNumber = s.PhoneNumber,
                DOB = s.DOB,
                Rating = db.Reviews
                    .Where(r => r.ReviewFor_ID == s.Sitter_ID && r.ReviewForRole == "Sitter")
                    .Average(r => (decimal?)r.Rating) ?? 0
            };
            return Ok(dto);
        }
        private bool HasConflictingJobs(int sitterId, DateTime date, List<int> requiredSlotIds, bool requireCommuteBuffer = true)
        {
            var conflictSlots = new HashSet<int>(requiredSlotIds);

            if (requireCommuteBuffer && requiredSlotIds.Any())
            {
                int minSlot = requiredSlotIds.Min();
                if (minSlot > 1)
                    conflictSlots.Add(minSlot - 1);
            }

            bool hasConflict = db.Jobs.Any(j =>
                j.AssignedSitter_ID == sitterId &&
                (j.Status == "Assigned" || j.Status == "In Progress") &&
                DbFunctions.TruncateTime(j.JobDate) == DbFunctions.TruncateTime(date) &&
                db.JobTimeSlots.Any(js =>
                    js.Job_ID == j.Job_ID &&
                    js.Slot_ID.HasValue &&
                    conflictSlots.Contains(js.Slot_ID.Value)
                )
            );

            return hasConflict;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}