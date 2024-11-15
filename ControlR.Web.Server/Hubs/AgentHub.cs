﻿using System.Net.Sockets;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using Microsoft.AspNetCore.SignalR;
using DeviceDto = ControlR.Libraries.Shared.Dtos.ServerApi.DeviceDto;

namespace ControlR.Web.Server.Hubs;

public class AgentHub(
  AppDb appDb,
  IHubContext<ViewerHub, IViewerHubClient> viewerHub,
  ISystemTime systemTime,
  IServerStatsProvider serverStatsProvider,
  IConnectionCounter connectionCounter,
  IWebHostEnvironment hostEnvironment,
  ILogger<AgentHub> logger) : HubWithItems<IAgentHubClient>, IAgentHub
{
  private readonly AppDb _appDb = appDb;
  private readonly IConnectionCounter _connectionCounter = connectionCounter;
  private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;
  private readonly ILogger<AgentHub> _logger = logger;
  private readonly IServerStatsProvider _serverStatsProvider = serverStatsProvider;
  private readonly ISystemTime _systemTime = systemTime;
  private readonly IHubContext<ViewerHub, IViewerHubClient> _viewerHub = viewerHub;

  private DeviceDto? Device
  {
    get => GetItem<DeviceDto?>(null);
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
        var updated = cachedDevice with
        {
          IsOnline = false,
          LastSeen = _systemTime.Now
        };

        var device = await _appDb.AddOrUpdateDevice(updated);
        await SendDeviceUpdate(device);
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

  public async Task<Result<DeviceDto>> UpdateDevice(DeviceDto deviceDto)
  {
    try
    {
      if (_hostEnvironment.IsDevelopment() && deviceDto.TenantId == Guid.Empty)
      {
        var lastTenant = await _appDb.Tenants
          .OrderByDescending(x => x.CreatedAt)
          .FirstOrDefaultAsync();

        if (lastTenant is null)
        {
          return Result.Fail<DeviceDto>("No tenants found.");
        }

        deviceDto = deviceDto with { TenantId = lastTenant.Id };
      }

      if (deviceDto.TenantId == Guid.Empty)
      {
        return Result.Fail<DeviceDto>("Invalid tenant ID.");
      }

      if (!await _appDb.Tenants.AnyAsync(x => x.Id == deviceDto.TenantId))
      {
        await Clients.Client(Context.ConnectionId).UninstallAgent("Invalid tenant ID.");
        return Result.Fail<DeviceDto>("Invalid tenant ID.");
      }

      deviceDto = deviceDto with
      {
        IsOnline = true,
        LastSeen = _systemTime.Now,
        ConnectionId = Context.ConnectionId
      };

      var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;
      if (remoteIp is not null)
      {
        if (remoteIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
          deviceDto = deviceDto with { PublicIpV6 = remoteIp.ToString() };
        }
        else
        {
          deviceDto = deviceDto with { PublicIpV4 = remoteIp.ToString() };
        }
      }
      
      var deviceEntity = await _appDb.AddOrUpdateDevice(deviceDto);
      if (Device is null)
      {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.GetTenantDevicesGroupName(deviceEntity.TenantId));
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.GetDeviceGroupName(deviceEntity.Id, deviceEntity.TenantId));
        if (deviceEntity.Tags is { Count: > 0 } tags)
        {
          foreach (var tag in tags)
          {
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.GetTagGroupName(tag.Id, deviceEntity.TenantId));
          }
        }
      }

      Device = deviceEntity.ToDto();

      await SendDeviceUpdate(deviceEntity);

      return Result.Ok(Device);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while updating device.");
      return Result.Fail<DeviceDto>("An error occurred while updating the device.");
    }
  }

  private async Task SendDeviceUpdate(Device device)
  {
    var dto = device.ToDto();
    
    await _viewerHub.Clients
      .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.DeviceSuperUser, device.TenantId))
      .ReceiveDeviceUpdate(dto);

    if (device.Tags is null)
    {
      return;
    }
    
    var groupNames = device.Tags.Select(x => HubGroupNames.GetTagGroupName(x.Id, x.TenantId));
    await _viewerHub.Clients.Groups(groupNames).ReceiveDeviceUpdate(dto);
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