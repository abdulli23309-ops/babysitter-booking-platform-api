using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http;
using WebApplication2.DTOs;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [RoutePrefix("api/notifications")]
    public class NotificationsController : ApiController
    {
        // Helper to get a fresh connection string from EF context
        private string GetConnectionString()
        {
            using (var ctx = new BabySitterBooking_and_BabyMinderEntities())
            {
                return ctx.Database.Connection.ConnectionString;
            }
        }

        // GET api/notifications?userId=9&userRole=Parent
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetNotifications(int userId, string userRole)
        {
            var list = new List<NotificationDto>();

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                var cmd = new SqlCommand(
                    @"SELECT Notification_ID, UserID, UserRole, Message, IsRead, CreatedAt, Type
                      FROM Notification
                      WHERE UserID = @UserID AND UserRole = @UserRole
                      ORDER BY CreatedAt DESC", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@UserRole", userRole);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new NotificationDto
                        {
                            NotificationId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            UserRole = reader.GetString(2),
                            Message = reader.GetString(3),
                            IsRead = reader.GetBoolean(4),
                            CreatedAt = reader.GetDateTime(5),
                            Type = reader.IsDBNull(6) ? "" : reader.GetString(6)
                        });
                    }
                }
            }

            return Ok(list);
        }

        // PUT api/notifications/5/read
        [HttpPut]
        [Route("{id}/read")]
        public IHttpActionResult MarkAsRead(int id)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                var cmd = new SqlCommand("UPDATE Notification SET IsRead = 1 WHERE Notification_ID = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // DELETE api/notifications/clear?userId=9&userRole=Parent
        [HttpDelete]
        [Route("clear")]
        public IHttpActionResult ClearAll(int userId, string userRole)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                var cmd = new SqlCommand("DELETE FROM Notification WHERE UserID = @UserID AND UserRole = @UserRole", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@UserRole", userRole);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // POST api/notifications  (internal use)
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateNotification([FromBody] NotificationDto dto)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                var cmd = new SqlCommand(
                    @"INSERT INTO Notification (UserID, UserRole, Message, Type)
                      VALUES (@UserID, @UserRole, @Message, @Type);", conn);
                cmd.Parameters.AddWithValue("@UserID", dto.UserId);
                cmd.Parameters.AddWithValue("@UserRole", dto.UserRole);
                cmd.Parameters.AddWithValue("@Message", dto.Message);
                cmd.Parameters.AddWithValue("@Type", dto.Type ?? "");
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok();
        }
    }
}