using ControlR.Shared;
using ControlR.Shared.Dtos;
using ControlR.Shared.Enums;
using ControlR.Shared.Helpers;
using ControlR.Shared.Interfaces;
using MessagePack;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Devices.Common.Services;
public interface IHubConnectionBase
{
    event EventHandler<SignedPayloadDto>? DtoReceived;
    HubConnectionState ConnectionState { get; }
    bool IsConnected { get; }

    Task ReceiveDto(SignedPayloadDto dto);
    Task Stop(CancellationToken cancellationToken);
}

public abstract class HubConnectionBase : IHubConnectionBase
{
    private readonly ILogger<HubConnectionBase> _baseLogger;
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationToken _cancellationToken;
    private Func<string, Task> _onConnectFailure = reason => Task.CompletedTask;
    private HubConnection? _connection;

    public HubConnectionBase(
        IServiceScopeFactory scopeFactory,
        ILogger<HubConnectionBase> logger)
    {
        _scopeFactory = scopeFactory;
        _baseLogger = logger;
    }

    public event EventHandler<SignedPayloadDto>? DtoReceived;

    public HubConnectionState ConnectionState => _connection?.State ?? HubConnectionState.Disconnected;
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    protected HubConnection Connection => _connection ?? throw new Exception("You must start the connection first.");

    public Task Connect(
        string hubUrl,
        Action<HubConnection> connectionConfig,
        Action<HttpConnectionOptions> optionsConfig,
        CancellationToken cancellationToken)
    {
        return Connect(hubUrl, connectionConfig, optionsConfig, _onConnectFailure, cancellationToken);
    }

    public async Task Connect(
        string hubUrl,
        Action<HubConnection> connectionConfig,
        Action<HttpConnectionOptions> optionsConfig,
        Func<string, Task> onConnectFailure,
        CancellationToken cancellationToken)
    {
        if (_connection is not null &&
            _connection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        _cancellationToken = cancellationToken;
        _onConnectFailure = onConnectFailure;

        using var scope = _scopeFactory.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IHubConnectionBuilder>();

        _connection = builder
            .WithUrl(hubUrl, options =>
            {
                optionsConfig(options);
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
            })
            .WithAutomaticReconnect(new RetryPolicy())
            .Build();

        _connection.On<SignedPayloadDto>(nameof(ReceiveDto), ReceiveDto);
        _connection.Reconnecting += HubConnection_Reconnecting;
        _connection.Reconnected += HubConnection_Reconnected;
        _connection.Closed += HubConnection_Closed;

        connectionConfig.Invoke(_connection);

        await StartConnection();
    }

    public Task ReceiveDto(SignedPayloadDto dto)
    {
        DtoReceived?.Invoke(this, dto);
        return Task.CompletedTask;
    }

    public async Task Reconnect(string hubUrl,
        Action<HubConnection> connectionConfig,
        Action<HttpConnectionOptions> optionsConfig)
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
        }

        await Connect(hubUrl, connectionConfig, optionsConfig, _onConnectFailure, _cancellationToken);
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            await _connection.StopAsync(cancellationToken);
        }
    }

    protected async Task WaitForConnection()
    {
        await WaitHelper.WaitForAsync(() => IsConnected, TimeSpan.MaxValue);
    }

 
    private Task HubConnection_Closed(Exception? arg)
    {
        _baseLogger.LogWarning(arg, "Hub connection closed.");
        return Task.CompletedTask;
    }
    private Task HubConnection_Reconnected(string? arg)
    {
        _baseLogger.LogInformation("Reconnected to desktop hub.  New connection ID: {id}", arg);
        return Task.CompletedTask;
    }

    private Task HubConnection_Reconnecting(Exception? arg)
    {
        _baseLogger.LogInformation(arg, "Reconnecting to desktop hub.");
        return Task.CompletedTask;
    }

    private async Task StartConnection()
    {
        if (_connection is null)
        {
            _baseLogger.LogWarning("Connection shouldn't be null here.");
            return;
        }

        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                _baseLogger.LogInformation("Connecting to server.");

                await _connection.StartAsync(_cancellationToken);

                _baseLogger.LogInformation("Connected to server.");

                break;
            }
            catch (HttpRequestException ex)
            {
                _baseLogger.LogWarning(ex, "Failed to connect to server.  Status Code: {code}", ex.StatusCode);
                await _onConnectFailure.Invoke($"Communication failure.  Status Code: {ex.StatusCode}");
            }
            catch (Exception ex)
            {
                _baseLogger.LogError(ex, "Error in hub connection.");
                await _onConnectFailure.Invoke($"Connection error.  Message: {ex.Message}");
            }
            await Task.Delay(3_000, _cancellationToken);
        }
    }
    private class RetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(3);
        }
    }
}
