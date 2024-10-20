﻿namespace ControlR.Libraries.Agent.Interfaces;

public interface IAgentInstaller
{
  Task Install(Uri? serverUri = null, Guid? deviceGroupId = null);

  Task Uninstall();
}