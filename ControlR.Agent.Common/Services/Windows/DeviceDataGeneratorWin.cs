﻿using System.Runtime.Versioning;
using ControlR.Agent.Common.Interfaces;
using ControlR.Agent.Common.Models;
using ControlR.Agent.Common.Services.Base;
using ControlR.Devices.Native.Services;
using ControlR.Libraries.DevicesNative.Windows;
using Microsoft.Extensions.Options;

namespace ControlR.Agent.Common.Services.Windows;

[SupportedOSPlatform("windows6.0.6000")]
internal class DeviceDataGeneratorWin(
  IWin32Interop win32Interop,
  ISystemEnvironment environmentHelper,
  ICpuUtilizationSampler cpuUtilizationSampler,
  IOptionsMonitor<AgentAppOptions> appOptions,
  ILogger<DeviceDataGeneratorWin> logger)
  : DeviceDataGeneratorBase(environmentHelper, cpuUtilizationSampler, appOptions, logger), IDeviceDataGenerator
{
  private readonly ILogger<DeviceDataGeneratorWin> _logger = logger;

  public async Task<DeviceModel> CreateDevice(Guid deviceId)
  {
    try
    {
      var (usedStorage, totalStorage) = GetSystemDriveInfo();
      var (usedMemory, totalMemory) = await GetMemoryInGb();

      var currentUsers = win32Interop.GetActiveSessions()
        .Select(x => x.Username)
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .ToArray();

      var drives = GetAllDrives();
      var agentVersion = GetAgentVersion();

      return GetDeviceBase(
        deviceId,
        currentUsers,
        drives,
        usedStorage,
        totalStorage,
        usedMemory,
        totalMemory,
        agentVersion);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting device info.");
      throw;
    }
  }

  public Task<(double usedGB, double totalGB)> GetMemoryInGb()
  {
    double totalGb = 0;
    double freeGb = 0;

    try
    {
      var memoryStatus = new MemoryStatusEx();

      if (win32Interop.GlobalMemoryStatus(ref memoryStatus))
      {
        freeGb = Math.Round((double)memoryStatus.ullAvailPhys / 1024 / 1024 / 1024, 2);
        totalGb = Math.Round((double)memoryStatus.ullTotalPhys / 1024 / 1024 / 1024, 2);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while getting device memory.");
    }

    return Task.FromResult((totalGb - freeGb, totalGB: totalGb));
  }
}