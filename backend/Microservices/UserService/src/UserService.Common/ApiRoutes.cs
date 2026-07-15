using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Common;
public class ApiRoutes
{
    public const string UserIdParam = "{userId}";

    public const string Users = "users";
    public const string AddUser = $"{Users}/add-user";
    public const string GetUserById = $"{Users}/getById";
    public const string DeleteUser = $"{Users}/{UserIdParam}";
    public const string UpdateUser = $"{Users}/update-user";

    public const string GetRoles = $"{Users}/get-roles";

    public const string SignIn = $"{Users}/sign-in";
    public const string RefreshToken = $"{Users}/refresh-token";
    public const string VerifyPassword = $"{Users}/verify-user-password";

}
