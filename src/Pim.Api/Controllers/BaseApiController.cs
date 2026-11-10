using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Pim.Api.Controllers;

public static class Papeis
{
    public const string Admin = "Admin";
    public const string Gestores = "Admin,Gestor";
}

[ApiController]
[Authorize]
[EnableRateLimiting("geral")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase { }
