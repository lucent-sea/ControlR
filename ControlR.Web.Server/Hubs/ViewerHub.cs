﻿using System.Runtime.CompilerServices;
using ControlR.Web.Client.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ControlR.Web.Server.Hubs;

[Authorize]
public class ViewerHub(
  IHubContext<AgentHub, IAgentHubClient> agentHub,
  IServerStatsProvider serverStatsProvider,
  IConnectionCounter connectionCounter,
  IAlertStore alertStore,
  IIpApi ipApi,
  IWsBridgeApi wsBridgeApi,
  IOptionsMonitor<ApplicationOptions> appOptions,
  ILogger<ViewerHub> logger) : HubWithItems<IViewerHubClient>, IViewerHub
{
  public Task<bool> CheckIfServerAdministrator()
  {
    return IsServerAdmin().AsTaskResult();
  }

  public async Task<Result> ClearAlert()
  {
    using var scope = logger.BeginMemberScope();

    if (!VerifyIsAdmin())
    {
      return Result.Fail("Unauthorized.");
    }

    return await alertStore.ClearAlert();
  }

  public async Task<Result<TerminalSessionRequestResult>> CreateTerminalSession(string agentConnectionId,
    TerminalSessionRequest requestDto)
  {
    try
    {
      return await agentHub.Clients
        .Client(agentConnectionId)
        .CreateTerminalSession(requestDto);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while creating terminal session.");
      return Result.Fail<TerminalSessionRequestResult>("An error occurred.");
    }
  }

  public async Task<Result<AgentAppSettings>> GetAgentAppSettings(string agentConnectionId)
  {
    try
    {
      return await agentHub.Clients.Client(agentConnectionId).GetAgentAppSettings();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while getting agent appsettings.");
      return Result.Fail<AgentAppSettings>("Failed to get agent app settings.");
    }
  }

  public async Task<Result<AlertBroadcastDto>> GetCurrentAlert()
  {
    try
    {
      return await alertStore.GetCurrentAlert();
    }
    catch (Exception ex)
    {
      return Result
        .Fail<AlertBroadcastDto>(ex, "Failed to get current alert.")
        .Log(logger);
    }
  }

  public async Task<Result<ServerStatsDto>> GetServerStats()
  {
    try
    {
      if (!VerifyIsAdmin())
      {
        return Result.Fail<ServerStatsDto>("Unauthorized.");
      }

      return await serverStatsProvider.GetServerStats();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while getting agent count.");
      return Result.Fail<ServerStatsDto>("Failed to get agent count.");
    }
  }

  public async Task<Uri?> GetWebSocketBridgeOrigin()
  {
    try
    {
      if (!appOptions.CurrentValue.UseExternalWebSocketBridge ||
          appOptions.CurrentValue.ExternalWebSocketHosts.Count == 0)
      {
        return null;
      }

      var ipAddress = Context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString();
      if (string.IsNullOrWhiteSpace(ipAddress))
      {
        return null;
      }

      var result = await ipApi.GetIpInfo(ipAddress);
      if (!result.IsSuccess)
      {
        return null;
      }


      var ipInfo = result.Value;

      if (ipInfo.Status == IpApiResponseStatus.Fail)
      {
        logger.LogError("IpApi returned a failed status message.  Message: {IpMessage}", ipInfo.Message);
        return null;
      }

      var location = new Coordinate(ipInfo.Lat, ipInfo.Lon);
      var closest = CoordinateHelper.FindClosestCoordinate(location, appOptions.CurrentValue.ExternalWebSocketHosts);
      if (closest.Origin is null || !await wsBridgeApi.IsHealthy(closest.Origin))
      {
        return null;
      }

      return closest.Origin;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while getting WebSocket bridge URI.");
      return null;
    }
  }

  public async Task<WindowsSession[]> GetWindowsSessions(string agentConnectionId)
  {
    try
    {
      return await agentHub.Clients.Client(agentConnectionId).GetWindowsSessions();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while getting Windows sessions from agent.");
      return [];
    }
  }

  public async Task<Result> RequestStreamingSession(
    string agentConnectionId,
    StreamerSessionRequestDto sessionRequestDto)
  {
    try
    {
      // TODO: Get user name from IUserStore
      //var name = Context.User?.Identity?.Name;
      //sessionRequestDto = sessionRequestDto with { ViewerName = name ?? "" };

      var sessionSuccess = await agentHub.Clients
        .Client(agentConnectionId)
        .CreateStreamingSession(sessionRequestDto);

      if (!sessionSuccess)
      {
        return Result.Fail("Failed to request a streaming session from the agent.");
      }

      return Result.Ok();
    }
    catch (Exception ex)
    {
      return Result.Fail(ex);
    }
  }

  public async Task<Result> SendAgentAppSettings(string agentConnectionId, AgentAppSettings appSettings)
  {
    try
    {
      return await agentHub.Clients.Client(agentConnectionId).ReceiveAgentAppSettings(appSettings);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while sending agent appsettings.");
      return Result.Fail("Failed to send agent app settings.");
    }
  }

  public async Task<Result> SendAlertBroadcast(AlertBroadcastDto alertDto)
  {
    try
    {
      using var scope = logger.BeginMemberScope();

      var storeResult = await alertStore.StoreAlert(alertDto);
      if (!storeResult.IsSuccess)
      {
        return storeResult;
      }

      await Clients.All.ReceiveAlertBroadcast(alertDto);
      return Result.Ok();
    }
    catch (Exception ex)
    {
      return Result
        .Fail(ex, "Failed to send agent app settings.")
        .Log(logger);
    }
  }

  public async Task SendDtoToAgent(string deviceId, DtoWrapper wrapper)
  {
    using var scope = logger.BeginMemberScope();

    await agentHub.Clients.Group(deviceId).ReceiveDto(wrapper);
  }

  public Task SendDtoToUserGroups(DtoWrapper wrapper)
  {
    // TODO: Implement this.
    throw new NotImplementedException();
  }

  public async Task<Result> SendTerminalInput(string agentConnectionId, TerminalInputDto dto)
  {
    try
    {
      return await agentHub.Clients.Client(agentConnectionId).ReceiveTerminalInput(dto);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while sending terminal input.");
      return Result.Fail("Agent could not be reached.");
    }
  }

  public override async Task OnConnectedAsync()
  {
    try
    {
      connectionCounter.IncrementViewerCount();
      await SendUpdatedConnectionCountToAdmins();

      await base.OnConnectedAsync();

      if (Context.User is null)
      {
        logger.LogWarning("User is null.  Authorize tag should have prevented this.");
        return;
      }

      // TODO: Add user to groups.
      //await Groups.AddToGroupAsync(Context.ConnectionId, publicKey);

      if (IsServerAdmin())
      {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.ServerAdministrators);

        var getResult = await serverStatsProvider.GetServerStats();
        if (getResult.IsSuccess)
        {
          await Clients.Caller.ReceiveServerStats(getResult.Value);
        }
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error during viewer connect.");
    }
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    try
    {
      connectionCounter.DecrementViewerCount();
      await SendUpdatedConnectionCountToAdmins();
      // TODO: Remove from groups.
      await base.OnDisconnectedAsync(exception);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error during viewer disconnect.");
    }
  }

  private bool IsServerAdmin()
  {
    return Context.User?.IsAdministrator() ?? false;
  }

  private async Task SendUpdatedConnectionCountToAdmins()
  {
    try
    {
      var getResult = await serverStatsProvider.GetServerStats();

      if (getResult.IsSuccess)
      {
        await Clients
          .Group(HubGroupNames.ServerAdministrators)
          .ReceiveServerStats(getResult.Value);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while sending updated agent connection count to admins.");
    }
  }

  private bool VerifyIsAdmin([CallerMemberName] string callerMember = "")
  {
    if (IsServerAdmin())
    {
      return true;
    }

    var userName = Context.User?.Identity?.Name;
    logger.LogCritical(
      "Admin verification failed when invoking member {MemberName}. User: {UserName}",
      callerMember,
      userName);

    return false;
  }
}