﻿using Bitbound.SimpleMessenger;
using ControlR.Devices.Common.Messages;
using ControlR.Viewer.Models.Messages;

namespace ControlR.Devices.Common.Extensions;

internal static class IMessengerExtensions
{
    public static void RegisterGenericMessage(
        this IMessenger messenger,
        object recipient,
        GenericMessageKind kind,
        Action handler)
    {
        messenger.Register<GenericMessage<GenericMessageKind>>(recipient, (subscriber, message) =>
        {
            if (kind == message.Value)
            {
                handler();
            }

            return Task.CompletedTask;
        });
    }

    public static void RegisterGenericMessage(
        this IMessenger messenger,
        object recipient,
        GenericMessageKind kind,
        Func<Task> handler)
    {
        messenger.Register<GenericMessage<GenericMessageKind>>(recipient, async (subscriber, message) =>
        {
            if (kind == message.Value)
            {
                await handler();
            }
        });
    }

    public static void RegisterGenericMessage(
        this IMessenger messenger,
        object recipient,
        Action<object, GenericMessageKind> handler)
    {
        messenger.Register<GenericMessage<GenericMessageKind>>(recipient, (subscriber, message) =>
        {
            handler(subscriber, message.Value);
            return Task.CompletedTask;
        });
    }

    public static void RegisterGenericMessage(
        this IMessenger messenger,
        object recipient,
        Func<object, GenericMessageKind, Task> handler)
    {
        messenger.Register<GenericMessage<GenericMessageKind>>(recipient, async (subscriber, message) =>
        {
            await handler(subscriber, message.Value);
        });
    }

    public static void SendGenericMessage(this IMessenger messenger, GenericMessageKind kind)
    {
        messenger.Send(new GenericMessage<GenericMessageKind>(kind));
    }
}