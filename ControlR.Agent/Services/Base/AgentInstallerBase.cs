﻿using ControlR.Agent.Models;
using ControlR.Devices.Common.Services;
using ControlR.Shared;
using ControlR.Shared.Extensions;
using ControlR.Shared.Helpers;
using ControlR.Shared.Services.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ControlR.Agent.Services.Base;

internal abstract class AgentInstallerBase(
    IFileSystem _fileSystem,
    IDownloadsApi _downloadsApi,
    ISettingsProvider _settings,
    ILogger<AgentInstallerBase> _logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    protected async Task UpdateAppSettings(string installDir, Uri? serverUri, string? authorizedKey, int? vncPort, bool? autoRunVnc)
    {
        using var _ = _logger.BeginMemberScope();

        var appSettings = new AppSettings();

        var appsettingsPath = Path.Combine(installDir, "appsettings.json");

        if (_fileSystem.FileExists(appsettingsPath))
        {
            var content = await _fileSystem.ReadAllTextAsync(appsettingsPath);
            var deserialized = JsonSerializer.Deserialize<AppSettings>(content);
            if (deserialized is not null)
            {
                appSettings = deserialized;
            }
            _logger.LogInformation("Existing app settings found.  Using it as a base.");
            _logger.LogInformation("Key Count: {num}", appSettings.AppOptions.AuthorizedKeys.Count);
        }
        else
        {
            _logger.LogInformation("No appsettings found.  Creating a new one.");
        }

        var updatedServerUri =
            serverUri?.ToString().TrimEnd('/') ??
            appSettings.AppOptions.ServerUri?.TrimEnd('/') ??
            AppConstants.ServerUri;
        _logger.LogInformation("Setting server URI to {ServerUri}.", updatedServerUri);
        appSettings.AppOptions.ServerUri = updatedServerUri;

        var updatedVncPort = vncPort ?? appSettings.AppOptions.VncPort ?? 5900;
        _logger.LogInformation("Setting VNC port to {VncPort}.", updatedVncPort);
        appSettings.AppOptions.VncPort = updatedVncPort;

        var updatedAutoRunVnc = autoRunVnc ?? appSettings.AppOptions.AutoRunVnc ?? false;
        _logger.LogInformation("Setting auto-run of VNC to {AutoRunVnc}.", updatedAutoRunVnc);
        // TODO: Remove this override later.
        appSettings.AppOptions.AutoRunVnc = true;

        if (!string.IsNullOrWhiteSpace(authorizedKey) &&
            !appSettings.AppOptions.AuthorizedKeys.Contains(authorizedKey))
        {
            _logger.LogInformation("Adding key passed in from arguments.");
            appSettings.AppOptions.AuthorizedKeys.Add(authorizedKey);
            _logger.LogInformation("Key Count: {num}", appSettings.AppOptions.AuthorizedKeys.Count);
        }

        _logger.LogInformation("Removing duplicates.");
        appSettings.AppOptions.AuthorizedKeys.RemoveDuplicates();
        _logger.LogInformation("Key Count: {num}", appSettings.AppOptions.AuthorizedKeys.Count);

        _logger.LogInformation("Removing empties.");
        appSettings.AppOptions.AuthorizedKeys.RemoveAll(string.IsNullOrWhiteSpace);
        _logger.LogInformation("Key Count: {num}", appSettings.AppOptions.AuthorizedKeys.Count);

        if (string.IsNullOrWhiteSpace(appSettings.AppOptions.DeviceId))
        {
            _logger.LogInformation("DeviceId is empty.  Generating new one.");
            appSettings.AppOptions.DeviceId = RandomGenerator.CreateDeviceToken();
        }

        _logger.LogInformation("Writing results to disk.");
        await _fileSystem.WriteAllTextAsync(appsettingsPath, JsonSerializer.Serialize(appSettings, _jsonOptions));
    }

    protected async Task WriteEtag(string installDir)
    {
        _logger.LogInformation("Retrieving ETag for installed app.");
        var agentDownloadUri = $"{_settings.ServerUri}downloads/{AppConstants.AgentFileName}";
        var etagResult = await _downloadsApi.GetAgentEtag(agentDownloadUri);
        if (etagResult.IsSuccess)
        {
            var etagPath = Path.Combine(installDir, "etag.txt");
            _logger.LogInformation("Writing ETag to file at path {path}.", etagPath);
            await _fileSystem.WriteAllTextAsync(etagPath, etagResult.Value.Trim());
        }
    }
}