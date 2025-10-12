using System.Reflection;
using Microsoft.AspNetCore.SignalR;

public abstract class AliasedHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        return base.OnDisconnectedAsync(exception);
    }

    // This function gets called dynamically by name from reflection
    public async Task InvokeAliased(string methodName, params object[] args)
    {
        var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<HubMethodNameAttribute>();
            if (attr != null && attr.Name == methodName)
            {
                var result = method.Invoke(this, args);
                if (result is Task task)
                    await task;
                return;
            }
        }

        await Clients.Caller.SendAsync("error", $"Unknown method: {methodName}");
    }
}
