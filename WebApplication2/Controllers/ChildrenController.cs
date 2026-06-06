using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApplication2.DTOs;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [RoutePrefix("api/parent")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ChildrenController : ApiController
    {
        private BabySitterBooking_and_BabyMinderEntities db = new BabySitterBooking_and_BabyMinderEntities();

        // ---------------- UPDATE CHILD (multipart) ----------------
        [HttpPut]
        [Route("child/{childId}")]
        public IHttpActionResult UpdateChild(int childId)
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // Read form fields
                string childName = httpRequest.Form["ChildName"];
                string dobStr = httpRequest.Form["DOB"];
                string gender = httpRequest.Form["Gender"];
                string special = httpRequest.Form["SpecialRequirements"];
                string guardianName = httpRequest.Form["GuardianName"];
                string guardianRelation = httpRequest.Form["GuardianRelation"];
                string guardianContact = httpRequest.Form["GuardianContact"];
                bool useDefault = httpRequest.Form["UseDefaultPicture"] == "true";

                using (var db = new BabySitterBooking_and_BabyMinderEntities())
                {
                    var child = db.Children.FirstOrDefault(c => c.Child_ID == childId);
                    if (child == null) return NotFound();

                    // Update text fields if provided
                    if (!string.IsNullOrWhiteSpace(childName))
                        child.ChildName = childName.Trim();

                    if (!string.IsNullOrWhiteSpace(dobStr) && DateTime.TryParse(dobStr, out DateTime dob))
                        child.DOB = dob;

                    if (!string.IsNullOrWhiteSpace(gender))
                        child.Gender = gender;

                    if (special != null)
                        child.SpecialRequirements = special;

                    if (guardianName != null)
                        child.GuardianName = guardianName;

                    if (guardianRelation != null)
                        child.GuardianRelation = guardianRelation;

                    if (guardianContact != null)
                        child.GuardianContact = guardianContact;

                    // Handle image update (only if a new file is uploaded)
                    if (!useDefault && httpRequest.Files.Count > 0)
                    {
                        var postedFile = httpRequest.Files[0];
                        if (postedFile != null && postedFile.ContentLength > 0)
                        {
                            string ext = Path.GetExtension(postedFile.FileName).ToLower();
                            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                                return BadRequest("Only JPG/PNG images are allowed.");

                            string fileName = Guid.NewGuid().ToString() + ext;
                            string folderPath = HttpContext.Current.Server.MapPath("~/Images/Children/");
                            if (!Directory.Exists(folderPath))
                                Directory.CreateDirectory(folderPath);

                            string fullPath = Path.Combine(folderPath, fileName);
                            postedFile.SaveAs(fullPath);
                            child.PictureAddress = "Children/" + fileName;
                        }
                    }
                    else if (useDefault)
                    {
                        child.PictureAddress = "Children/default_child.jpg";
                    }
                    // else keep existing picture

                    db.SaveChanges();
                    return Ok(new { message = "Child updated successfully", childId = child.Child_ID });
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }
    }
    }