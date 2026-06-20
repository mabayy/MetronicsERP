using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpMetronic.Infrastructure;

/// <summary>Registrasi layanan Infrastructure (EF Core + Identity) ke DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' tidak ditemukan.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<IDocumentNumberService, DocumentNumberService>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<ITaxService, TaxService>();

        return services;
    }
}
