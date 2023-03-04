using ControlR.Server.Auth;
using ControlR.Server.Data;
using ControlR.Server.Extensions;
using ControlR.Shared;
using ControlR.Shared.DbEntities;
using ControlR.Shared.Dtos;
using ControlR.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ControlR.Server.Api;

[Route("api/[controller]")]
[ApiController]
public partial class KeyController : ControllerBase
{
    [HttpGet("verify")]
    [Authorize]
    public IActionResult Verify()
    {
        return Ok();
    }
}