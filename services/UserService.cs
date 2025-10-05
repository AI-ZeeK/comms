namespace Comms.Services
{
    public class UserService
    {
        public object GetDummyUser()
        {
            return new
            {
                Id = Guid.NewGuid(),
                Name = "Test User age 10",
                Email = "test@example.com"
            };
        }
    }
}
