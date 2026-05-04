namespace SharedLibrary.DTOs.Responses
{
    public class UserResponse
    {
        public int Id { get; }
        public string Name { get; }

        public UserResponse(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
