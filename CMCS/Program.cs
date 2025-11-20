using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CMCS.Services;
using CMCS;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// This enables session with a 30 min timeout
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// This configures Identity for authentication
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<FileEncryptionService>();

var app = builder.Build();

// This ensures the database is created and the roles exist on startup
using (var scope = app.Services.CreateScope())
{
    var svcProvider = scope.ServiceProvider;
    var db = svcProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated(); 
    await EnsureRoles(svcProvider); // this creates standard roles if missing
    await CreateHRRole(svcProvider); // this create default HR user if missing
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); 
    app.UseHsts(); 
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // this is required for Identity
app.UseAuthorization();

app.UseSession(); // this enables session 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// This ensures the main roles exists 
async Task EnsureRoles(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "HR", "Lecturer", "Coordinator", "Manager" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role)); // create role if missing
        }
    }
}

// This creates a default HR user if missing
async Task CreateHRRole(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("HR"))
        await roleManager.CreateAsync(new IdentityRole("HR")); // ensure HR role exists

    var hrUser = await userManager.FindByEmailAsync("hr@system.com");

    if (hrUser == null)
    {
        hrUser = new ApplicationUser
        {
            UserName = "hr@system.com",
            Email = "hr@system.com",
            FirstName = "System",
            LastName = "Admin"
        };

        await userManager.CreateAsync(hrUser, "Admin#1234"); 
        await userManager.AddToRoleAsync(hrUser, "HR"); 
    }
}
