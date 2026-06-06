using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApplication2.DTOs;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/review")]
    public class ReviewController : ApiController
    {
        private BabySitterBooking_and_BabyMinderEntities db = new BabySitterBooking_and_BabyMinderEntities();

        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddReview(ReviewDTO reviewDto)
        {
            try
            {
                // ✅ FIXED: Saving using the new Review schema
                var review = new Review
                {
                    Job_ID = reviewDto.Job_ID,
                    Reviewer_ID = reviewDto.Reviewer_ID,
                    ReviewerRole = reviewDto.ReviewerRole,
                    ReviewFor_ID = reviewDto.ReviewFor_ID,
                    ReviewForRole = reviewDto.ReviewForRole,
                    Rating = reviewDto.Rating,
                    Comment = reviewDto.Comment,
                    CreatedAt = DateTime.Now
                };

                db.Reviews.Add(review);
                db.SaveChanges();

                // 💡 NOTE: We no longer update sitter.Rating because the column is dropped.
                // The average is calculated dynamically in the GetMatchingSitters / Filter methods.

                return Ok(new { message = "Review Added Successfully", success = true });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("sitter/{sitterId}")]
        public IHttpActionResult GetSitterRating(int sitterId)
        {
            try
            {
                // ✅ FIXED: Role-based lookup
                var avg = db.Reviews
                    .Where(r => r.ReviewFor_ID == sitterId && r.ReviewForRole == "Sitter")
                    .Average(r => (decimal?)r.Rating) ?? 0;

                return Ok(avg);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("parent/{parentId}")]
        public IHttpActionResult GetParentRating(int parentId)
        {
            try
            {
                // ✅ FIXED: Use ReviewFor_ID and check Role
                var avgRating = db.Reviews
                    .Where(r => r.ReviewFor_ID == parentId && r.ReviewForRole == "Parent")
                    .Average(r => (decimal?)r.Rating) ?? 0;

                return Ok(avgRating);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpGet]
        [Route("user/{userId}/{role}")]
        public IHttpActionResult GetUserReviews(int userId, string role)
        {
            try
            {
                var reviews = db.Reviews
                    .Where(r => r.ReviewFor_ID == userId && r.ReviewForRole == role)
                    .Select(r => new {
                        r.Rating,
                        r.Comment,
                        r.CreatedAt,
                        ReviewerName = r.ReviewerRole == "Parent"
                            ? db.Parents.Where(p => p.Parent_ID == r.Reviewer_ID).Select(p => p.FullName).FirstOrDefault()
                            : db.Babysitters.Where(b => b.Sitter_ID == r.Reviewer_ID).Select(b => b.FullName).FirstOrDefault()
                    })
                    .ToList();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}