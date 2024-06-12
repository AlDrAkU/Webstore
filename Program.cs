using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Globalization;
using Webstore.Controllers;
using Webstore.Data;
using Webstore.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews().AddViewLocalization();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddTransient<RateLimiterMiddleware>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("nl")
    };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
// Add Swagger services
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Webstore API", Version = "v1" });
});

builder.Logging.AddConsole();

var app = builder.Build();
app.UseRequestLocalization();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Define your roles here
    string[] roles = new string[] { "Admin", "Seller", "Customer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Define your users here
    IdentityUser admin = new IdentityUser { UserName = "admin@admin.com", Email = "admin@admin.com" };
    IdentityUser seller = new IdentityUser { UserName = "seller@store.com", Email = "seller@store.com" };
    IdentityUser customer = new IdentityUser { UserName = "user@store.com", Email = "user@store.com" };

    // Add to database and assign roles
    await userManager.CreateAsync(admin, "Admin123!");
    await userManager.CreateAsync(seller, "Seller123!");
    await userManager.CreateAsync(customer, "Customer123!");

    await userManager.AddToRoleAsync(admin, "Admin");
    await userManager.AddToRoleAsync(seller, "Seller");
    await userManager.AddToRoleAsync(customer, "Customer");
}
app.UseMiddleware<RateLimiterMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Test voor de db verbinding
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var services = scope.ServiceProvider;
    if (dbContext.Database.CanConnect())
    {
        Console.WriteLine("Database connection successful!");
        try
        {

            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            // Log the error or handle it as needed
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or initializing the database.");
        }
    }
    else
    {

        Console.WriteLine("Error connecting to the database.");
    }
}

app.Run();
