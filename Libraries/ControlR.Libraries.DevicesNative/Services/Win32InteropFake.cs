﻿using ControlR.Devices.Native.Services;
using ControlR.Libraries.DevicesNative.Windows;
using ControlR.Libraries.Shared.Dtos.StreamerDtos;
using ControlR.Libraries.Shared.Enums;
using ControlR.Libraries.Shared.Models;
using ControlR.Libraries.Shared.Primitives;
using System.Diagnostics;

namespace ControlR.Libraries.DevicesNative.Services;
public class Win32InteropFake : IWin32Interop
{
    public bool CreateInteractiveSystemProcess(string commandLine, int targetSessionId, bool hiddenWindow, out Process? startedProcess)
    {
        throw new NotImplementedException();
    }

    public List<WindowsSession> GetActiveSessions()
    {
        throw new NotImplementedException();
    }

    public List<WindowsSession> GetActiveSessionsCsWin32()
    {
        throw new NotImplementedException();
    }

    public string? GetClipboardText()
    {
        throw new NotImplementedException();
    }

    public WindowsCursor GetCurrentCursor()
    {
        throw new NotImplementedException();
    }

    public bool GetCurrentThreadDesktop(out string desktopName)
    {
        throw new NotImplementedException();
    }

    public bool GetInputDesktop(out string desktopName)
    {
        throw new NotImplementedException();
    }

    public bool GetThreadDesktop(uint threadId, out string desktopName)
    {
        throw new NotImplementedException();
    }

    public string GetUsernameFromSessionId(uint sessionId)
    {
        throw new NotImplementedException();
    }

    public bool GlobalMemoryStatus(ref MemoryStatusEx lpBuffer)
    {
        throw new NotImplementedException();
    }

    public void InvokeCtrlAltDel()
    {
        throw new NotImplementedException();
    }

    public Result InvokeKeyEvent(string key, bool isPressed)
    {
        throw new NotImplementedException();
    }

    public void InvokeMouseButtonEvent(int x, int y, int button, bool isPressed)
    {
        throw new NotImplementedException();
    }

    public void InvokeWheelScroll(int x, int y, int scrollY, int scrollX)
    {
        throw new NotImplementedException();
    }

    public void MovePointer(int x, int y, MovePointerType moveType)
    {
        throw new NotImplementedException();
    }

    public nint OpenInputDesktop()
    {
        throw new NotImplementedException();
    }

    public void ResetKeyboardState()
    {
        throw new NotImplementedException();
    }

    public void SetClipboardText(string? text)
    {
        throw new NotImplementedException();
    }

    public bool SwitchToInputDesktop()
    {
        throw new NotImplementedException();
    }

    public void TypeText(string text)
    {
        throw new NotImplementedException();
    }
}
