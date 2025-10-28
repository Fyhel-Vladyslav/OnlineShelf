namespace UserService.src.Common.DTOs
{
    public class UserDto
    {    // Primary Key for the database
        public Guid Id { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
    }
}
