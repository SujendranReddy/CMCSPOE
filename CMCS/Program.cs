using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CMCS.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<FileEncryptionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var svcProvider = scope.ServiceProvider;
    var db = svcProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    await CreateHRRole(svcProvider);
}

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

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

async Task CreateHRRole(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("HR"))
        await roleManager.CreateAsync(new IdentityRole("HR"));

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
