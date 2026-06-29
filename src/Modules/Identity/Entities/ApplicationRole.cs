using Microsoft.AspNetCore.Identity;

namespace LinkUp.Modules.Identity.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
