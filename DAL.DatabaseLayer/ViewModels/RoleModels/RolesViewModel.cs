namespace DAL.DatabaseLayer.ViewModels.RoleModels;

public class CreateRoleViewModel
{
    public string RoleName { get; set; } = string.Empty;
}

public class UserRoleViewModel
{
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class RoleViewModel
{
    public string Email { get; set; } = string.Empty;
}

public class ClaimViewModel
{
    public string Email { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
}

