﻿using ControlR.Agent.Interfaces;
using ControlR.Shared.Dtos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ControlR.Agent.Services.Fakes;
internal class StreamerUpdaterFake(ILogger<StreamerUpdaterFake> _logger) : BackgroundService, IStreamerUpdater
{
    public Task<bool> EnsureLatestVersion(StreamerSessionRequestDto requestDto, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Platform not supported for desktop streaming.");
        return Task.FromResult(false);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
