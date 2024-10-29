﻿using ControlR.Libraries.Shared.Dtos.HubDtos;
using Microsoft.AspNetCore.SignalR;
using System.Net.Sockets;

namespace ControlR.Web.Server.Hubs;

public class AgentHub(
  AppDb _appDb,
  IHubContext<ViewerHub, IViewerHubClient> _viewerHub,
  ISystemTime _systemTime,
  IServerStatsProvider _serverStatsProvider,
  IConnectionCounter _connectionCounter,
  IWebHostEnvironment _hostEnvironment,
  ILogger<AgentHub> _logger) : HubWithItems<IAgentHubClient>, IAgentHub
{
  private DeviceResponseDto? Device
  {
    get => GetItem<DeviceResponseDto?>(null);
    set => SetItem(value);
  }

  private Guid? TenantId
  {
    get => GetItem<Guid?>(null);
    set => SetItem(value);
  }

  public override async Task OnConnectedAsync()
  {
    try
    {
      _connectionCounter.IncrementAgentCount();
      await SendUpdatedConnectionCountToAdmins();
      await base.OnConnectedAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during device connect.");
    }
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    try
    {
      _connectionCounter.DecrementAgentCount();
      await SendUpdatedConnectionCountToAdmins();

      if (Device is { } cachedDevice)
      {
        cachedDevice.IsOnline = false;
        cachedDevice.LastSeen = _systemTime.Now;
        await _viewerHub.Clients
          .Group(HubGroupNames.ServerAdministrators)
          .ReceiveDeviceUpdate(cachedDevice);

        await _appDb.AddOrUpdateDevice(cachedDevice);

        await SendDeviceUpdate();
      }


      await base.OnDisconnectedAsync(exception);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during device disconnect.");
    }
  }

  public async Task SendStreamerDownloadProgress(StreamerDownloadProgressDto progressDto)
  {
    await _viewerHub.Clients.Client(progressDto.ViewerConnectionId).ReceiveStreamerDownloadProgress(progressDto);
  }

  public async Task SendTerminalOutputToViewer(string viewerConnectionId, TerminalOutputDto outputDto)
  {
    try
    {
      await _viewerHub.Clients
        .Client(viewerConnectionId)
        .ReceiveTerminalOutput(outputDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending terminal output to viewer.");
    }
  }

  public async Task<Result<DeviceResponseDto>> UpdateDevice(DeviceRequestDto device)
  {
    try
    {
      if (_hostEnvironment.IsDevelopment() && device.TenantId == Guid.Empty)
      {
        var firstTenant = await _appDb.Tenants
          .OrderBy(x => x.CreatedAt)
          .FirstOrDefaultAsync();

        if (firstTenant is null)
        {
          return Result.Fail<DeviceResponseDto>("No tenants found.");
        }

        device.TenantId = firstTenant.Id;
      }

      if (device.TenantId == Guid.Empty)
      {
        return Result.Fail<DeviceResponseDto>("Invalid tenant ID.");
      }

      if (!await _appDb.Tenants.AnyAsync(x => x.Id == device.TenantId))
      {
        // TODO: Send uninstall command.
        return Result.Fail<DeviceResponseDto>("Invalid tenant ID.");
      }

      device.IsOnline = true;
      device.LastSeen = _systemTime.Now;
      device.ConnectionId = Context.ConnectionId;

      var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;
      if (remoteIp is not null)
      {
        if (remoteIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
          device.PublicIpV6 = remoteIp.ToString();
        }
        else
        {
          device.PublicIpV4 = remoteIp.ToString();
        }
      }


      var deviceEntity = await _appDb.AddOrUpdateDevice(device);
      TenantId = deviceEntity.TenantId;
      Device = deviceEntity.ToDto();

      Device.ConnectionId = Context.ConnectionId;

      await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.GetDeviceGroupName(Device.Id));

      await SendDeviceUpdate();

      return Result.Ok(Device);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while updating device.");
      return Result.Fail<DeviceResponseDto>("An error occurred while updating the device.");
    }
  }

  private async Task SendDeviceUpdate()
  {
    if (Device is null)
    {
      return;
    }

    if (TenantId.HasValue)
    {
      await _viewerHub.Clients
        .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.DeviceSuperUser, TenantId.Value))
        .ReceiveDeviceUpdate(Device);
    }
  }
  private async Task SendUpdatedConnectionCountToAdmins()
  {
    try
    {
      var statsResult = await _serverStatsProvider.GetServerStats();
      if (statsResult.IsSuccess)
      {
        await _viewerHub.Clients
          .Group(HubGroupNames.ServerAdministrators)
          .ReceiveServerStats(statsResult.Value);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending updated agent connection count to admins.");
    }
  }
}