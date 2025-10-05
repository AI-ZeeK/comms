using Microsoft.AspNetCore.Mvc;
using Comms.Constants;
using Comms.Guards;

namespace Comms.Controllers
{
    [ApiController]
    [Route(ApiConstants.API_VERSION + "/[controller]")]
    public class ProtectedController : ControllerBase
    {
        [HttpGet("user-only")]
        // [UserGuard]
        [ServiceFilter(typeof(UserGuard))]
        public IActionResult UserOnly()
        {
            var userInfo = HttpContext.Items["UserInfo"];
            return Ok(new
            {
                message = "This endpoint requires user authentication",
                userInfo = userInfo
            });
        }

        [HttpGet("admin-only")]
        // [AdminGuard]
        [ServiceFilter(typeof(AdminGuard))]
        public IActionResult AdminOnly()
        {
            var adminInfo = HttpContext.Items["AdminInfo"];
            return Ok(new
            {
                message = "This endpoint requires admin authentication",
                adminInfo = adminInfo
            });
        }

        [HttpGet("public")]
        public IActionResult Public()
        {
            return Ok(new { message = "This endpoint is public and requires no authentication" });
        }
    }
}

