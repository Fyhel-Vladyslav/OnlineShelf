namespace UserService.src.UserService.Repository.EfCore.Entities;
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public List<UserRoleLink> UserRoles { get; set; } = new();
}