using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApplication2.DTOs;

namespace WebApplication2.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/cry-detection")]
    public class CryDetectionController : ApiController
    {
        // Use the same connection string as the rest of your app
        private readonly string connStr =
            ConfigurationManager.ConnectionStrings["BabySitterBooking_and_BabyMinderEntities"]
                                      ?.ConnectionString;

        // POST api/cry-detection
        [HttpPost]
        [Route("")]
        public IHttpActionResult PostCryAlert([FromBody] CryAlertDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid payload.");

            // 1. Generate a unique Jitsi room name
            string roomName = $"baby-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";

            // 2. Save to database
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        @"INSERT INTO CryAlert (Id, Timestamp, Level, RoomName, JobId, ParentId, BabysitterId)
                          VALUES (@id, @ts, @level, @room, @jobId, @parentId, @babysitterId)", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                        cmd.Parameters.AddWithValue("@ts", DateTime.Parse(dto.Timestamp));
                        cmd.Parameters.AddWithValue("@level", dto.Level);
                        cmd.Parameters.AddWithValue("@room", roomName);
                        cmd.Parameters.AddWithValue("@jobId", (object)dto.JobId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@parentId", (object)dto.ParentId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@babysitterId", (object)dto.BabysitterId ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                // 3. (Later you can add SignalR / push notification here)
                return Ok(new { roomName, message = "Alert received" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/cry-detection/latest?parentId=34
        [HttpGet]
        [Route("latest")]
        public IHttpActionResult GetLatest(int? parentId = null)
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT TOP 1 * FROM CryAlert";
                    if (parentId.HasValue)
                        sql += " WHERE ParentId = @parentId";
                    sql += " ORDER BY CreatedAt DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (parentId.HasValue)
                            cmd.Parameters.AddWithValue("@parentId", parentId.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Ok(new
                                {
                                    timestamp = reader["Timestamp"].ToString(),
                                    roomName = reader["RoomName"].ToString(),
                                    level = reader["Level"].ToString()
                                });
                            }
                            return NotFound();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}