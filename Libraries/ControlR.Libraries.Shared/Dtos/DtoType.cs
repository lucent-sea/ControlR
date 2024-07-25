﻿namespace ControlR.Libraries.Shared.Dtos;

public enum DtoType
{
    None = 0,
    IdentityAttestation = 1,
    StreamingSessionRequest = 2,
    GetWindowsSessions = 3,
    DeviceUpdateRequest = 4,
    TerminalSessionRequest = 5,
    CloseTerminalRequest = 7,
    PowerStateChange = 8,
    TerminalInput = 9,
    StartRdpProxy = 10,
    GetAgentAppSettings = 11,
    SendAppSettings = 12,
    WakeDevice = 13,
    GetAgentCountRequest = 14,
    SendAlertBroadcast = 15,
    ClearAlerts = 16,
    ChangeDisplays = 17,
    CloseStreamingSession = 19,
    InvokeCtrlAltDel = 20,
    ClipboardChanged = 21,
    TriggerAgentUpdate = 22,
    DisplayData = 23,
    DesktopChanged = 24,
    DesktopRequest = 25,
    MovePointer = 26,
    MouseButtonEvent = 27,
    KeyEvent = 28,
    TypeText = 29,
    ResetKeyboardState = 30,
    WheelScroll = 31,
    ScreenRegion = 32
}