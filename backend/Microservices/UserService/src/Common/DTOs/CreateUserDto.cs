namespace UserService.src.Common
{
    public class CreateUserDto
    {  
        public Guid Id { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
    }
}
