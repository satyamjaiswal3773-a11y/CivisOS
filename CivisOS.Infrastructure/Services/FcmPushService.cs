using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CivisOS.Infrastructure.Services;

public interface IFcmPushService
{
    Task SendAsync(IReadOnlyList<string> deviceTokens, string title, string body, CancellationToken cancellationToken = default);
}

public class FcmSettings
{
    public const string SectionName = "FcmSettings";
    public bool Enabled { get; set; }
    public string? ServerKey { get; set; }
}

/// <summary>
/// Lightweight FCM sender. When disabled or misconfigured, logs payloads (dev-friendly stub).
/// </summary>
public class FcmPushService : IFcmPushService
{
    private readonly FcmSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FcmPushService> _logger;

    public FcmPushService(
        IOptions<FcmSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<FcmPushService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(
        IReadOnlyList<string> deviceTokens,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (deviceTokens.Count == 0)
        {
            return;
        }

        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ServerKey))
        {
            _logger.LogInformation(
                "FCM stub: would push to {Count} device(s). Title={Title} Body={Body}",
                deviceTokens.Count,
                title,
                body);
            return;
        }

        var client = _httpClientFactory.CreateClient("fcm");
        foreach (var token in deviceTokens)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
            request.Headers.TryAddWithoutValidation("Authorization", $"key={_settings.ServerKey}");
            request.Content = JsonContent.Create(new
            {
                to = token,
                notification = new { title, body },
                data = new { title, body }
            });

            try
            {
                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("FCM push failed ({Status}): {Content}", response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FCM push exception for token ending {Suffix}", token[^Math.Min(6, token.Length)..]);
            }
        }
    }
}
