namespace UserService.src.Common
{
    public class CreateUserDto
    {  
        public string Login { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public string? Avatar { get; set; }
    }
}
