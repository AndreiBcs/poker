namespace User_Microservice.DTOs
{
    // ba deci ca sa intelegeti DTO (data transfer object) 
    // e practiv ce trimiti / primesti prin API
    // ca sa nu folosesti direct un obiect de User care contine informatii
    // private folosite de baza de date pentru autentificare (parola si email)
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Level { get; set; } 
    }
}
