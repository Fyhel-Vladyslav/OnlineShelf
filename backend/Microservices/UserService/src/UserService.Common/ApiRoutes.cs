namespace UserService.src.UserService.Common;
public class ApiRoutes
{

    public const string Users = "users";
    public const string AddUser = $"{Users}/add-user";
    public const string GetUserById = $"{Users}/getById";
    public const string DeleteUser = "delete-user/{userId}";
    public const string UpdateUser = $"{Users}/update-user";

    public const string GetRoles = $"{Users}/get-roles";

    public const string SignIn = $"{Users}/sign-in";
    public const string VerifyPassword = $"{Users}/verify-user-password";

}
