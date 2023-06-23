using System.Text.Json;
using Microsoft.JSInterop;

public class LocalStorageAccessor : IAsyncDisposable
{
    private Lazy<IJSObjectReference> _accessorJsRef = new();
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageAccessor(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task WaitForReference()
    {
        if (_accessorJsRef.IsValueCreated is false)
        {
            _accessorJsRef = new(await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/LocalStorageAccessor.js"));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_accessorJsRef.IsValueCreated)
        {
            await _accessorJsRef.Value.DisposeAsync();
        }
    }

    public async Task<T> GetValueAsync<T>(string key)
    {
        await WaitForReference();
        var result = await _accessorJsRef.Value.InvokeAsync<T>("get", key);

        return result;
    }

    public async Task<JsonElement> GetKeys()
    {
        await WaitForReference();
        var result = (JsonElement)await _accessorJsRef.Value.InvokeAsync<object>("getKeys");

        return result;
    }

    public async Task SetValueAsync<T>(string key, T value)
    {
        await WaitForReference();
        await _accessorJsRef.Value.InvokeVoidAsync("set", key, value);
    }

    public async Task Clear()
    {
        await WaitForReference();
        await _accessorJsRef.Value.InvokeVoidAsync("clear");
    }

    public async Task RemoveAsync(string key)
    {
        await WaitForReference();
        await _accessorJsRef.Value.InvokeVoidAsync("remove", key);
    }

    public async Task RenameKey(string oldName, string newName)
    {
        await WaitForReference();
        var value = await GetValueAsync<string>(oldName);
        await SetValueAsync(newName, value);
        await RemoveAsync(oldName);
    }
}