using CorpLinkBaseMinimal.Auth;
using CorpLinkBaseMinimal.Data;
using CorpLinkBaseMinimal.Hubs;
using CorpLinkBaseMinimal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContextFactory<MessengerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<MessengerDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    // На localhost cookie не различает порты: одно имя = одна сессия на всех портах.
    // Если Auth:CookieName не задан, имя строится из порта первого URL (разные процессы на 51671 и 51672 = разные cookie).
    options.Cookie.Name = ResolveAuthCookieName(builder.Configuration);
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IMessengerRepository, MessengerRepository>();
builder.Services.AddScoped<MessengerService>();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/login", async (HttpContext context, SignInManager<User> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var usernameOrEmail = form["UsernameOrEmail"].ToString();
    var password = form["Password"].ToString();
    var returnUrl = ReturnUrlHelper.Sanitize(form["ReturnUrl"].ToString());

    var user = await signInManager.UserManager.FindByNameAsync(usernameOrEmail);
    if (user is null)
        user = await signInManager.UserManager.FindByEmailAsync(usernameOrEmail);

    if (user is null)
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}&error=credentials");

    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
    if (!result.Succeeded)
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}&error=credentials");

    return Results.Redirect(returnUrl);
});

app.MapPost("/auth/register", async (HttpContext context, UserManager<User> userManager, SignInManager<User> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var userName = form["UserName"].ToString();
    var displayName = form["DisplayName"].ToString();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();
    var returnUrl = ReturnUrlHelper.Sanitize(form["ReturnUrl"].ToString());

    var user = new User
    {
        UserName = userName,
        Email = email,
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
        return Results.Redirect($"/register?returnUrl={Uri.EscapeDataString(returnUrl)}&error=create");

    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.Redirect(returnUrl);
});

app.MapPost("/auth/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapBlazorHub();
app.MapHub<ChatHub>("/chathub");
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessengerDbContext>>();
    await using var dbContext = await dbFactory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
}

app.Run();

static string ResolveAuthCookieName(IConfiguration configuration)
{
    var explicitName = configuration["Auth:CookieName"];
    if (!string.IsNullOrWhiteSpace(explicitName))
        return explicitName.Trim();

    var urls = configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
    foreach (var raw in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) && uri is { Scheme: "http" or "https", Port: > 0 })
            return $".CorpLink.Auth.{uri.Port}";
    }

    return ".CorpLink.Auth";
}
