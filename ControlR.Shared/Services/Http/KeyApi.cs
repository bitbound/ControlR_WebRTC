using Microsoft.Extensions.Logging;

namespace ControlR.Shared.Services.Http;

public interface IKeyApi
{
    Task<Result> VerifyKeys();
}

internal class KeyApi : IKeyApi
{
    private readonly HttpClient _client;
    private readonly ILogger<KeyApi> _logger;

    public KeyApi(
        HttpClient client,
        ILogger<KeyApi> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Result> VerifyKeys()
    {
        try
        {
            var response = await _client.GetAsync($"/api/key/verify");
            response.EnsureSuccessStatusCode();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while verifying authentication.");
            return Result.Fail(ex);
        }
    }
}