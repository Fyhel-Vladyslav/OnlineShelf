namespace UserService.src.Common
{
    public class CreateUserDto
    {    // Primary Key for the database
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
    }
}
