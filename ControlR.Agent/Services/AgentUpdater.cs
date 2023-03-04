using Microsoft.Extensions.Hosting;
using ControlR.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net;
using ControlR.Devices.Common;
using ControlR.Agent.Interfaces;
using ControlR.Devices.Common.Services;
using ControlR.Shared;
using ControlR.Shared.Services.Http;
using System.Security.Cryptography.X509Certificates;
using ControlR.Shared.Extensions;

namespace ControlR.Agent.Services;

internal interface IAgentUpdater : IHostedService
{
    Task CheckForUpdate();
}

internal class AgentUpdater : IAgentUpdater
{
    private readonly SemaphoreSlim _checkForUpdatesLock = new(1, 1);
    private readonly IDownloadsApi _downloadsApi;
    private readonly IEnvironmentHelper _environmentHelper;
    private readonly IFileSystem _fileSystem;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AgentUpdater> _logger;
    private readonly IProcessInvoker _processInvoker;
    private readonly System.Timers.Timer _updateTimer = new(TimeSpan.FromHours(6).TotalMilliseconds);
    public AgentUpdater(
        HttpClient httpClient,
        IDownloadsApi downloadsApi,
        IFileSystem fileSystem,
        IProcessInvoker processInvoker,
        IEnvironmentHelper environmentHelper,
        ILogger<AgentUpdater> logger)
    {
        _httpClient = httpClient;
        _downloadsApi = downloadsApi;
        _fileSystem = fileSystem;
        _processInvoker = processInvoker;
        _environmentHelper = environmentHelper;
        _logger = logger;
    }

    public async Task CheckForUpdate()
    {
        using var _ = _logger.BeginMemberScope();

        if (!await _checkForUpdatesLock.WaitAsync(0))
        {
            _logger.LogWarning("Failed to acquire lock in agent updater.  Aborting check.");
            return;
        }

        try
        {
            _logger.LogInformation("Beginning version check.");

            var downloadUrl = $"{AppConstants.ServerUri}/downloads/{AppConstants.AgentFileName}";
            var etagPath = Path.Combine(_environmentHelper.StartupDirectory, "etag.txt");

            using var request = new HttpRequestMessage(HttpMethod.Head, downloadUrl);

            if (_fileSystem.FileExists(etagPath))
            {
                var lastEtag = await _fileSystem.ReadAllTextAsync(etagPath);
                if (!string.IsNullOrWhiteSpace(lastEtag) &&
                   EntityTagHeaderValue.TryParse(lastEtag.Trim(), out var etag))
                {
                    _logger.LogInformation("Found existing etag {etag}.  Adding it to IfNoneMatch header.", etag);
                    request.Headers.IfNoneMatch.Add(etag);
                }
            }

            using var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                _logger.LogInformation("Version is current.");
                return;
            }

            if (string.IsNullOrWhiteSpace(response.Headers.ETag?.Tag))
            {
                _logger.LogCritical("New etag is empty.  Update cannot continue.");
                return;
            }

            _logger.LogInformation("Update found. Downloading update.");

            var tempFile = $"{Guid.NewGuid()}-{AppConstants.AgentFileName}";
            var tempDir = _fileSystem.CreateDirectory(Path.Combine(Path.GetTempPath(), "ControlR_Update")).FullName;
            var tempPath = Path.Combine(tempDir, tempFile);

            var result = await _downloadsApi.DownloadAgent(tempPath);
            if (!result.IsSuccess)
            {
                _logger.LogCritical("Download failed.  Aborting update.");
                return;
            }

            var cert = X509Certificate.CreateFromSignedFile(tempPath);
            var thumbprint = cert.GetCertHashString().Trim();

            if (!string.Equals(thumbprint, AppConstants.AgentCertificateThumbprint, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogCritical(
                    "The certificate thumbprint of the downloaded agent binary is invalid.  Aborting update.  " +
                    "Expected Thumbprint: {expected}.  Actual Thumbprint: {actual}.",
                    AppConstants.AgentCertificateThumbprint,
                    thumbprint);
                return;
            }

            _logger.LogInformation("Launching installer.");
            _processInvoker.Start(tempPath, "install");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking for updates.");
        }
        finally
        {
            _checkForUpdatesLock.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_environmentHelper.IsDebug)
        {
            return;
        }

        await CheckForUpdate();
        _updateTimer.Elapsed += UpdateTimer_Elapsed;
        _updateTimer.Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _updateTimer.Dispose();
        return Task.CompletedTask;
    }

    private async void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        await CheckForUpdate();
    }
}
