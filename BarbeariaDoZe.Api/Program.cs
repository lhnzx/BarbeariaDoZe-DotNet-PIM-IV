using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using BarbeariaDoZe.Negocio;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Barbearia")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Barbearia.");
var chave = builder.Configuration["Auth:SigningKey"] ?? "";
// Em desenvolvimento/apresentação local, gera uma chave aleatória quando nenhuma foi configurada.
if (Encoding.UTF8.GetByteCount(chave) < 32 && builder.Environment.IsDevelopment())
{
    chave = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    builder.Configuration["Auth:SigningKey"] = chave;
}
if (Encoding.UTF8.GetByteCount(chave) < 32) throw new InvalidOperationException("Configure Auth:SigningKey com pelo menos 32 bytes aleatórios; não salve a chave no Git.");
builder.Services.AddNegocio(connectionString);
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "BarbeariaDoZe",
        ValidateAudience = true,
        ValidAudience = "BarbeariaDoZe.Clientes",
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "name",
        RoleClaimType = "role"
    };
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
    {
        var p = ctx.Principal!;
        if (!int.TryParse(p.FindFirstValue("sub"), out var id)) { ctx.Fail("Sessão inválida."); return; }
        var usuario = await ctx.HttpContext.RequestServices.GetRequiredService<UsuarioNegocio>().ObterAsync(id);
        if (usuario is null || usuario.SenhaHash is null || usuario.SecurityStamp != p.FindFirstValue("stamp")) ctx.Fail("Sessão expirada.");
    }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("conta", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Barbearia do Zé - API",
        Version = "v1",
        Description = "Autentique-se em /api/conta/login. Envie Authorization: Bearer <token>. Reservas pertencem à conta autenticada."
    });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", doc)] = [] });
    o.OperationFilter<BarbeariaDoZe.Api.AcessoPublicoSwagger>();
});
var origens = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:8081", "http://127.0.0.1:8081"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new { erro = "Não foi possível concluir a operação. Tente novamente." });
}));
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseSwagger(); app.UseSwaggerUI(); app.UseCors(); app.UseRateLimiter();
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); app.Run();

