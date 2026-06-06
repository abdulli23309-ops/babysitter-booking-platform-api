using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace WebApplication2.Controllers
{
    [RoutePrefix("api/images")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ImageController : ApiController
    {
        [HttpGet]
        [Route("{type}/{filename}")]
        public HttpResponseMessage GetImage(string type, string filename)
        {
            // Determine folder based on type using a normal switch
            string folder;
            switch (type?.ToLower())
            {
                case "parents":
                    folder = "~/Images/Parents/";
                    break;
                case "sitters":
                    folder = "~/Images/Sitters/";
                    break;
                case "children":
                    folder = "~/Images/Children/";
                    break;
                default:
                    folder = "~/Images/";
                    break;
            }

            string path = HttpContext.Current.Server.MapPath(folder + filename);

            // If the requested image does not exist, fallback to default.jpg
            if (!File.Exists(path))
            {
                path = HttpContext.Current.Server.MapPath("~/Images/default.jpg");
                if (!File.Exists(path))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return response;
        }
    }
}