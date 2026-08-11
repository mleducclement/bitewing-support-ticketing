using Microsoft.AspNetCore.Mvc;

namespace bitewing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet(Name = "GetResponse")]
    public string Get()
    {
        return "Testing if CI is functional";
    }
}