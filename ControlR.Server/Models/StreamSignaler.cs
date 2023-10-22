﻿using System.Net.WebSockets;

namespace ControlR.Server.Models;

public class StreamSignaler(Guid sessionId)
{
    public SemaphoreSlim EndSignal { get; } = new(0, 2);
    public SemaphoreSlim NoVncViewerReady { get; } = new(0, 2);
    public WebSocket? NoVncWebsocket { get; internal set; }
    public Guid SessionId { get; init; } = sessionId;
    public IAsyncEnumerable<byte[]>? Stream { get; set; }
}