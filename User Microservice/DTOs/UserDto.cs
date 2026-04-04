namespace User_Microservice.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Level { get; set; }
    }
}
