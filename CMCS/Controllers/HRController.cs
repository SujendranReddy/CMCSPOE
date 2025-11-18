using CMCS;
using CMCS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "HR")]
public class HRController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public HRController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        var model = new List<UserCreateEditViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserCreateEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                HourlyRate = user.HourlyRate,
                MaxHoursPerMonth = user.MaxHoursPerMonth,
                Role = roles.FirstOrDefault()
            });
        }

        return View(model);
    }

    public IActionResult Create()
    {
        ViewBag.Roles = new SelectList(new[] { "HR", "Lecturer", "Coordinator", "Manager" });
        return View(new UserCreateEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateEditViewModel model)
    {
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        ViewBag.Roles = new SelectList(roles);

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError("Password", "A password is required when creating a user.");
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            HourlyRate = model.HourlyRate,
            MaxHoursPerMonth = model.MaxHoursPerMonth
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);

            return View(model);
        }

        if (!string.IsNullOrEmpty(model.Role))
        {
            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new IdentityRole(model.Role));

            await _userManager.AddToRoleAsync(user, model.Role);
        }

        TempData["Success"] = $"User {user.Email} created.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = new SelectList(new[] { "HR", "Lecturer", "Coordinator", "Manager" }, userRoles.FirstOrDefault());

        return View(new UserCreateEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            HourlyRate = user.HourlyRate,
            MaxHoursPerMonth = user.MaxHoursPerMonth,
            Role = userRoles.FirstOrDefault()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserCreateEditViewModel model)
    {
        var allRoles = new[] { "HR", "Lecturer", "Coordinator", "Manager" };
        ViewBag.Roles = allRoles.Select(r => new SelectListItem
        {
            Text = r,
            Value = r,
            Selected = r == model.Role
        }).ToList();

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.HourlyRate = model.HourlyRate;
        user.MaxHoursPerMonth = model.MaxHoursPerMonth;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new IdentityRole(model.Role));

            await _userManager.AddToRoleAsync(user, model.Role);
        }

        if (!string.IsNullOrEmpty(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, model.Password);
        }

        TempData["Success"] = $"User {user.Email} updated successfully.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

        var model = new UserCreateEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            HourlyRate = user.HourlyRate,
            MaxHoursPerMonth = user.MaxHoursPerMonth,
            Role = role
        };

        return View(model);
    }

    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

        var model = new UserCreateEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            HourlyRate = user.HourlyRate,
            MaxHoursPerMonth = user.MaxHoursPerMonth,
            Role = role
        };

        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
            return RedirectToAction("Index");

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(user);
    }
}
