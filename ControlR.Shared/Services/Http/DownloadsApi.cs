﻿using ControlR.Shared.IO;
using Microsoft.Extensions.Logging;

namespace ControlR.Shared.Services.Http;

internal interface IDownloadsApi
{
    Task<Result> DownloadAgent(string destinationPath);
    Task<Result> DownloadRemoteControl(string destinationPath);
    Task<Result> DownloadRemoteControlZip(string destinationPath, Func<double, Task>? onDownloadProgress);
    Task<Result<string>> GetAgentEtag();
}

internal class DownloadsApi(
    HttpClient client,
    ILogger<DownloadsApi> logger) : IDownloadsApi
{
    private readonly HttpClient _client = client;
    private readonly ILogger<DownloadsApi> _logger = logger;

    public async Task<Result> DownloadAgent(string destinationPath)
    {
        try
        {
            using var webStream = await _client.GetStreamAsync($"{AppConstants.ServerUri}/downloads/{AppConstants.AgentFileName}");
            using var fs = new FileStream(destinationPath, FileMode.Create);
            await webStream.CopyToAsync(fs);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while downloading agent.");
            return Result.Fail(ex);
        }
    }

    public async Task<Result<string>> GetAgentEtag()
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Head,
                $"{AppConstants.ServerUri}/downloads/{AppConstants.AgentFileName}");

            using var response = await _client.SendAsync(request);
            var etag = response.Headers.ETag?.Tag;

            if (string.IsNullOrWhiteSpace(etag))
            {
                return Result.Fail<string>("Etag from HEAD request is empty.");
            }

            return Result.Ok(etag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking agent etag.");
            return Result.Fail<string>(ex);
        }
    }

    public async Task<Result> DownloadRemoteControl(string destinationPath)
    {
        try
        {
            using var webStream = await _client.GetStreamAsync($"{AppConstants.ServerUri}/downloads/{AppConstants.RemoteControlFileName}");
            using var fs = new FileStream(destinationPath, FileMode.Create);
            await webStream.CopyToAsync(fs);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while downloading remote control client.");
            return Result.Fail(ex);
        }
    }

    public async Task<Result> DownloadRemoteControlZip(string destinationPath, Func<double, Task>? onDownloadProgress)
    {
        try
        {
            var zipUrl = $"{AppConstants.ServerUri}/downloads/{AppConstants.RemoteControlZipFileName}";

            using var message = new HttpRequestMessage(HttpMethod.Head, zipUrl);
            using var response = await _client.SendAsync(message);
            var totalSize = response.Content.Headers.ContentLength ?? 100_000_000; // rough estimate.

            using var webStream = await _client.GetStreamAsync(zipUrl);
            using var fs = new ReactiveFileStream(destinationPath, FileMode.Create);

            fs.TotalBytesWrittenChanged += async (sender, written) =>
            {
                if (onDownloadProgress is not null)
                {
                    var progress = (double)written / totalSize;
                    await onDownloadProgress.Invoke(progress);
                }
            };

            await webStream.CopyToAsync(fs);

            if (onDownloadProgress is not null)
            {
                await onDownloadProgress.Invoke(1);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while downloading remote control client.");
            return Result.Fail(ex);
        }
    }
}
