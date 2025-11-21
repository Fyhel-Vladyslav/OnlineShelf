using UserService.src.UserService.Common;

namespace UserService.src.UserService.Repository.EfCore.Entities;
public class UserRoleLink
{
    public Guid UserId { get; set; }
    public int RoleId { get; set; }

    public User User { get; set; }
    public Role Role { get; set; }
}
