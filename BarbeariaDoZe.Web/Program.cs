using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using BarbeariaDoZe.Negocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNegocio(builder.Configuration.GetConnectionString("Barbearia") ?? throw new InvalidOperationException("Configure ConnectionStrings:Barbearia."));
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", o =>
{
    o.LoginPath = "/Conta/Login"; o.AccessDeniedPath = "/Conta/AcessoNegado";
    o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    // Sessão renovada automaticamente enquanto o usuário navega (sem logout forçado a cada 30 minutos).
    o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = true;
    o.Events.OnValidatePrincipal = async ctx =>
    {
        var p = ctx.Principal!;
        if (!int.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { ctx.RejectPrincipal(); return; }
        var u = await ctx.HttpContext.RequestServices.GetRequiredService<UsuarioNegocio>().ObterAsync(id);
        if (u?.SenhaHash is null || u.SecurityStamp != p.FindFirstValue("stamp"))
        { ctx.RejectPrincipal(); await ctx.HttpContext.SignOutAsync("Cookies"); }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("conta", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
var cultura = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions { DefaultRequestCulture = new RequestCulture(cultura), SupportedCultures = [cultura], SupportedUICultures = [cultura] });
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Erro"); app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles(); app.UseRouting(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
// Inicialização explícita e idempotente; não há credenciais administrativas no repositório.
var emailAdmin = builder.Configuration["Bootstrap:AdminEmail"];
var senhaAdmin = builder.Configuration["Bootstrap:AdminPassword"];
if (!string.IsNullOrWhiteSpace(emailAdmin) && !string.IsNullOrWhiteSpace(senhaAdmin))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<UsuarioNegocio>().CriarAdministradorInicialAsync(emailAdmin,
        builder.Configuration["Bootstrap:AdminName"] ?? "Administrador", builder.Configuration["Bootstrap:AdminPhone"] ?? "11999999999", senhaAdmin);
}
// Modo demonstração (apresentação local): contas prontas para testar cliente e administrador.
if (builder.Configuration.GetValue<bool>("Demo:Ativo"))
{
    using var scope = app.Services.CreateScope();
    var usuarios = scope.ServiceProvider.GetRequiredService<UsuarioNegocio>();
    await usuarios.GarantirContaDemoAsync("admin@barbeariadoze.com", "Administrador", "16999990000", "Admin@Barbearia2026", "Admin");
    await usuarios.GarantirContaDemoAsync("cliente@barbeariadoze.com", "Cliente Demonstracao", "16999991111", "Cliente@Barbearia2026", "Cliente");
}
app.Run();

