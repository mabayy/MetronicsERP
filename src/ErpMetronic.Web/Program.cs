using ErpMetronic.Infrastructure;
using ErpMetronic.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// MVC + lapisan Infrastructure (EF Core + Identity)
builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);

// Izinkan token anti-forgery dikirim via header (dipakai reorder menu drag-and-drop).
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// Pengaturan cookie autentikasi
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Jalankan migrasi + seed data awal saat aplikasi start
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
