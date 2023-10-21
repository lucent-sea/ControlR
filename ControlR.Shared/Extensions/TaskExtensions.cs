﻿namespace ControlR.Shared.Extensions;

public static class TaskExtensions
{
    public static async void AndForget(this Task task, Func<Exception, Task>? exceptionHandler = null)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            if (exceptionHandler is null)
            {
                return;
            }

            try
            {
                await exceptionHandler(ex);
            }
            catch { }
        }
    }

    public static Task OrCompleted<T>(this T? value)
    {
        if (value is Task task)
        {
            return task;
        }
        return Task.CompletedTask;
    }
}