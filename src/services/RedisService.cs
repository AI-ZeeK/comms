using StackExchange.Redis;

public interface IConnectionTracker
{
    Task AddUserToRoomAsync(string userId, string roomName);
    Task RemoveUserFromRoomAsync(string userId, string roomName);
    Task<IEnumerable<string>> GetUserRoomsAsync(string userId);
    Task<bool> IsUserInRoomAsync(string userId, string roomName);
}

public class RedisConnectionTracker : IConnectionTracker
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisConnectionTracker(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    public async Task AddUserToRoomAsync(string userId, string roomName)
    {
        await _db.SetAddAsync($"user:{userId}:rooms", roomName);
    }

    public async Task RemoveUserFromRoomAsync(string userId, string roomName)
    {
        await _db.SetRemoveAsync($"user:{userId}:rooms", roomName);
    }

    public async Task<IEnumerable<string>> GetUserRoomsAsync(string userId)
    {
        var entries = await _db.SetMembersAsync($"user:{userId}:rooms");
        return entries.Select(e => e.ToString());
    }

    public async Task<bool> IsUserInRoomAsync(string userId, string roomName)
    {
        // Check if the roomName is in the user's Redis set
        return await _db.SetContainsAsync($"user:{userId}:rooms", roomName);
    }
}
