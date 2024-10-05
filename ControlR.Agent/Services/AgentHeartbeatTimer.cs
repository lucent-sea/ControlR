﻿using Microsoft.Extensions.Hosting;

namespace ControlR.Agent.Services;
internal class AgentHeartbeatTimer(
    IAgentHubConnection hubConnection,
    ISystemEnvironment _systemEnvironment,
    ILogger<AgentHeartbeatTimer> logger) : BackgroundService
{
  private readonly IAgentHubConnection _hubConnection = hubConnection;
  private readonly ILogger<AgentHeartbeatTimer> _logger = logger;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var delayTime = _systemEnvironment.IsDebug ?
        TimeSpan.FromSeconds(10) :
        TimeSpan.FromMinutes(5);

    using var timer = new PeriodicTimer(delayTime);

    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
      try
      {
        await _hubConnection.SendDeviceHeartbeat();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error while sending agent heartbeat.");
      }
    }
  }
}
