namespace UserService.src.Common
{
    public class UpdateUserDto
    {  
        public Guid Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public bool EmailVerified { get; set; }
        public int State { get; set; }
        public string? Avatar { get; set; }
    }
}
