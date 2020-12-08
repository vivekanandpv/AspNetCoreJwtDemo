using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreJwtDemo.Controllers
{
    [Authorize(Policy = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class SampleController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new {Message = "Sample : OK"});
        }

        [AllowAnonymous]
        [HttpGet("public")]
        public IActionResult GetPublic()
        {
            return Ok(new {Message = "Sample/Public : OK"});
        }
    }
}
