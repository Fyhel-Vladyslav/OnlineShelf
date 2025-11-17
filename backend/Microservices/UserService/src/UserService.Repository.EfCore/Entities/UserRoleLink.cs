using UserService.src.UserService.Common;

namespace UserService.src.UserService.Repository.EfCore.Entities
{
    public class UserRoleLink
    {
        public Guid UserId { get; set; }
        public UserRole Role { get; set; }
        public User User { get; set; }
    }
}
