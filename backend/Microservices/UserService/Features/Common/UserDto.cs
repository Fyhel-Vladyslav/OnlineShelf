namespace UserService.Features.Common
{
    public class UserDto
    {    // Primary Key for the database
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }
}
