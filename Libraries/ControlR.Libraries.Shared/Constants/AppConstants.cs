﻿using System.Diagnostics;
using ControlR.Libraries.Shared.Enums;
using ControlR.Libraries.Shared.Services;

namespace ControlR.Libraries.Shared.Constants;

public static class AppConstants
{
  public static Uri DevServerUri { get; } = new("http://localhost:5120");
  public static Uri ProdServerUri { get; } = new("https://app.controlr.app");

  public static string ExternalDownloadsUri => "https://controlr.app";

  public static string StreamerFileName
  {
    get
    {
      return SystemEnvironment.Instance.Platform switch
      {
        SystemPlatform.Windows => "ControlR.Streamer.exe",
        SystemPlatform.Linux => "ControlR.Streamer",
        _ => throw new PlatformNotSupportedException()
      };
    }
  }

  public static string StreamerZipFileName
  {
    get
    {
      return SystemEnvironment.Instance.Platform switch
      {
        SystemPlatform.Windows => "ControlR.Streamer.zip",
        _ => throw new PlatformNotSupportedException()
      };
    }
  }

  public static Uri ServerUri
  {
    get
    {
      if (OperatingSystem.IsWindows() && Debugger.IsAttached)
      {
        return DevServerUri;
      }

      return ProdServerUri;
    }
  }

  public static string GetAgentFileDownloadPath(RuntimeId runtime)
  {
    return runtime switch
    {
      RuntimeId.WinX64 => "/downloads/win-x64/ControlR.Agent.exe",
      RuntimeId.WinX86 => "/downloads/win-x86/ControlR.Agent.exe",
      RuntimeId.LinuxX64 => "/downloads/linux-x64/ControlR.Agent",
      RuntimeId.OsxX64 => "/downloads/osx-x64/ControlR.Agent",
      RuntimeId.OsxArm64 => "/downloads/osx-arm64/ControlR.Agent",
      _ => throw new PlatformNotSupportedException()
    };
  }

  public static string GetAgentFileName(SystemPlatform platform)
  {
    return platform switch
    {
      SystemPlatform.Windows => "ControlR.Agent.exe",
      SystemPlatform.Android => "ControlR.Agent.exe",
      SystemPlatform.Linux => "ControlR.Agent",
      SystemPlatform.MacOs => "ControlR.Agent",
      _ => throw new PlatformNotSupportedException()
    };
  }

  public static string GetStreamerFileDownloadPath(RuntimeId runtime)
  {
    return runtime switch
    {
      RuntimeId.WinX64 or RuntimeId.WinX86 => "/downloads/win-x86/ControlR.Streamer.zip",
      _ => throw new PlatformNotSupportedException()
    };
  }
}