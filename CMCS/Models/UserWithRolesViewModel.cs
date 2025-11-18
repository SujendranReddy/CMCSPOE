using CMCS;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

public class UserWithRolesViewModel
{
    public ApplicationUser User { get; set; }
    public IList<string> Roles { get; set; }
}
