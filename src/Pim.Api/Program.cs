using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pim.Api.Middlewares;
using Pim.Api.Servicos;
using Pim.Application;
using Pim.Application.Interfaces;
using Pim.Infrastructure;
using Pim.Infrastructure.Persistencia;
using Pim.Infrastructure.Seguranca;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddApplication();
builder.Services.AddInfrastructure(cfg);
builder.Services.AddSingleton(new AuthOptions
{
    MaxTentativas = cfg.GetValue("Auth:MaxTentativas", 5),
    BloqueioMinutos = cfg.GetValue("Auth:BloqueioMinutos", 15),
    RefreshDias = cfg.GetValue("Auth:RefreshDias", 7)
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

builder.Services.AddControllers(o => o.Filters.Add<ValidacaoFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true); // ValidacaoFilter formata os erros

// ---------- Autenticação JWT ----------
var jwt = JwtOptions.De(cfg);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "name", RoleClaimType = "role"
    };
    o.Events = new JwtBearerEvents
    {
        OnChallenge = ctx => { ctx.HandleResponse(); return ProblemaHttp.EscreverAsync(ctx.HttpContext, 401, "Autenticação necessária ou token inválido/expirado."); },
        OnForbidden = ctx => ProblemaHttp.EscreverAsync(ctx.HttpContext, 403, "Você não tem permissão para acessar este recurso.")
    };
});
builder.Services.AddAuthorization();

// ---------- CORS (origens separadas por vírgula em CORS_ORIGINS) ----------
var origens = (cfg["CORS_ORIGINS"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddPolicy("Padrao", p =>
{
    p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod();
}));

// ---------- Rate limiting ----------
static string Ip(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.OnRejected = async (ctx, _) => await ProblemaHttp.EscreverAsync(ctx.HttpContext, 429, "Muitas requisições. Tente novamente em instantes.");
    o.AddPolicy("geral", http => RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = cfg.GetValue("RateLimit:Geral", 300), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = cfg.GetValue("RateLimit:Login", 10), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PIM - API de Gerenciamento de Colaboradores e Tarefas",
        Version = "v1",
        Description = "API REST do PIM IV (UNIP/ADS). Faça login em /api/v1/auth/login e use o botão Authorize com o accessToken."
    });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Informe somente o access token (sem o prefixo 'Bearer')."
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
    var xml = Path.Combine(AppContext.BaseDirectory, "Pim.Api.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);
});

builder.Services.AddHealthChecks().AddDbContextCheck<PimDbContext>("banco");

var app = builder.Build();
app.UseCors("Padrao");

// ---------- Inicialização (Admin inicial) ----------
if (cfg.GetValue("Bootstrap:Habilitado", true))
{
    using var scope = app.Services.CreateScope();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Inicializacao");
    for (var tentativa = 1; ; tentativa++)
    {
        try
        {
            await DbInicializador.InicializarAsync(scope.ServiceProvider.GetRequiredService<PimDbContext>(),
                scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
                cfg["ADMIN_EMAIL"] ?? "admin@pim.local", cfg["ADMIN_PASSWORD"], log);
            break;
        }
        catch (Exception ex) when (tentativa < 15)
        {
            log.LogWarning("Banco indisponível (tentativa {T}/15): {Msg}", tentativa, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// ---------- Pipeline ----------
app.UseMiddleware<ExceptionMiddleware>();

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    if (!ctx.Request.Path.StartsWithSegments("/swagger"))   // Swagger UI usa scripts inline
        h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});

if (cfg.GetValue("Seguranca:ForcarHttps", app.Environment.IsProduction()))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(o => { o.SwaggerEndpoint("/swagger/v1/swagger.json", "PIM API v1"); o.RoutePrefix = "swagger"; });

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
