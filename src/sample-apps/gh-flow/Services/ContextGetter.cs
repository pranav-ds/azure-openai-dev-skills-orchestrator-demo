using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AI.DevTeam;

public interface IGetContext 
{
    Task<string> GetContext(ErrorDetail intent);
}
public class ContextGetter : IGetContext
{
    private readonly ServiceOptions _serviceOptions;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContextGetter> _logger;

    public ContextGetter(IOptions<ServiceOptions> serviceOptions, HttpClient httpClient, ILogger<ContextGetter> logger)
    {
        _serviceOptions = serviceOptions.Value;
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_serviceOptions.PrompterUrl);
        
    }
    public async Task<string> GetContext(ErrorDetail intent)
    {
        try
        {
            var body = new StringContent(JsonSerializer.Serialize(intent), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/context", body);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing code");
            throw;
        }
    }
}


public class ErrorDetail
{
    public string error { get; set; }
    public string file { get; set; }
    public string line { get; set; }
}
