using bitewing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bitewing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _db;
    
    public TestController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> GetPings()
        => Ok(await _db.Pings.ToListAsync());
}