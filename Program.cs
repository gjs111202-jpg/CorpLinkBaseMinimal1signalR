using CorpLinkBaseMinimal.Data;
using CorpLinkBaseMinimal.Hubs;
using CorpLinkBaseMinimal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Регистрация DbContextFactory
builder.Services.AddDbContextFactory<MessengerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- НАСТРОЙКА IDENTITY ---
// Добавляем Identity с нашими типами User и Role
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<MessengerDbContext>() // Говорим Identity использовать наш DbContext
    .AddDefaultTokenProviders();

// Настройка аутентификации через Cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";      // URL страницы входа
    options.LogoutPath = "/logout";    // URL для выхода
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// --- КОНЕЦ НАСТРОЙКИ IDENTITY ---

builder.Services.AddScoped<IMessengerRepository, MessengerRepository>();
builder.Services.AddScoped<MessengerService>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();   // Добавляет middleware для аутентификации
app.UseAuthorization();    // Добавляет middleware для авторизации

app.MapBlazorHub();
app.MapHub<ChatHub>("/chathub");
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MessengerDbContext>();
    dbContext.Database.Migrate();
}

app.Run();