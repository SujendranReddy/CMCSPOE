using CMCS;
using CMCS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class AccountController : Controller
{
    // This controller handles users and logins.
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

 
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

  
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
   
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Email and password are required.";
            return View(); // go back to login page with error if something is blank
        }

        // User signs in with the provided email & password
        var result = await _signInManager.PasswordSignInAsync(email, password, false, false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // Get a list of the role this user belongs to
                var roles = await _userManager.GetRolesAsync(user);

                // The user is redirected to their role's dashboard
                if (roles.Contains("Lecturer"))
                    return RedirectToAction("Dashboard", "Lecturer");
                else if (roles.Contains("Coordinator"))
                    return RedirectToAction("Dashboard", "Coordinator");
                else if (roles.Contains("Manager"))
                    return RedirectToAction("Dashboard", "Manager");
                else if (roles.Contains("HR"))
                    return RedirectToAction("Index", "HR");
            }

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Invalid login attempt.";
        return View(); // back to login page with error message if login is invalid
    }

    // This signs the user out and sends them back to the login page
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
}
