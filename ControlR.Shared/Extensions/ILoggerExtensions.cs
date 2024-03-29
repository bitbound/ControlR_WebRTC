﻿using Microsoft.Extensions.Logging;
using ControlR.Shared.Helpers;
using System.Runtime.CompilerServices;

namespace ControlR.Shared.Extensions;

public static class ILoggerExtensions
{
    public static IDisposable? BeginMemberScope<T>(this ILogger<T> logger, [CallerMemberName]string callerMemberName ="")
    {
        return logger.BeginScope(callerMemberName);
    }

    public static void LogResult<T, ResultT>(
        this ILogger<T> logger, 
        Result<ResultT> result, 
        [CallerMemberName]string callerName = "")
    {
        using var logScope = string.IsNullOrWhiteSpace(callerName) ?
            new NoopDisposable() :
            logger.BeginScope(callerName);


        if (result.IsSuccess)
        {
            logger.LogInformation("Successful result.");
        }
        else if (result.HadException)
        {
            logger.LogError(result.Exception, "Error result.");
        }
        else
        {
            logger.LogWarning("Failed result. Reason: {reason}", result.Reason);
        }
    }

    public static void LogResult<T>(
      this ILogger<T> logger,
      Result result,
      [CallerMemberName] string callerName = "")
    {
        using var logScope = string.IsNullOrWhiteSpace(callerName) ?
            new NoopDisposable() :
            logger.BeginScope(callerName);


        if (result.IsSuccess)
        {
            logger.LogInformation("Successful result.");
        }
        else if (result.HadException)
        {
            logger.LogError(result.Exception, "Error result.");
        }
        else
        {
            logger.LogWarning("Failed result. Reason: {reason}", result.Reason);
        }
    }
}
