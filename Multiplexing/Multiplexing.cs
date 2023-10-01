using System.Collections.Concurrent;

namespace Interview;

public sealed class ComplexRequestProcessor : IRequestProcessor
{
    private readonly ILowLevelNetworkAdapter _networkAdapter;

    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Response>> _pendingRequestIds = new();
    private readonly TimeSpan _requestTimeout;

    private CancellationTokenSource? _instanceCancellationTokenSource;

    public ComplexRequestProcessor(ILowLevelNetworkAdapter networkAdapter, TimeSpan requestTimeout)
    {
        if (requestTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));

        _networkAdapter = networkAdapter;
        _requestTimeout = requestTimeout;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _instanceCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await Task.Run(ProcessPendingRequestsAsync, cancellationToken);

        async Task ProcessPendingRequestsAsync()
        {
            try
            {
                while (!_instanceCancellationTokenSource.Token.IsCancellationRequested)
                {
                    var response = await _networkAdapter.ReadAsync(_instanceCancellationTokenSource.Token);

                    if (_pendingRequestIds.TryRemove(response.Id, out var tcs))
                        tcs.TrySetResult(response);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _instanceCancellationTokenSource?.Cancel();
        var tasks = _pendingRequestIds.Values.Select(s => s.Task).ToList();

        var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
        var allUnfinishedTasks = Task.WhenAll(tasks);

        await Task.WhenAny(cancelTask, allUnfinishedTasks);
        _pendingRequestIds.Clear();
        _instanceCancellationTokenSource?.Dispose();
    }

    public async Task<Response> SendAsync(Request request, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<Response>();
        _pendingRequestIds.TryAdd(request.Id, tcs);

        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            var realTask = _networkAdapter.WriteAsync(request, linkedTokenSource.Token);
            var delayTask = Task.Delay(_requestTimeout, cancellationToken);
            var completedTask = await Task.WhenAny(realTask, delayTask);
            if (completedTask == delayTask)
            {
                linkedTokenSource.Cancel();
                linkedTokenSource.Dispose();
                throw new TimeoutException("Request timed out.");
            }

            await realTask;
            return tcs.Task.Result;
        }
        finally
        {
            _pendingRequestIds.TryRemove(request.Id, out _);
        }
    }
}