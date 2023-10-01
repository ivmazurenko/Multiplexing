using System.Collections.Concurrent;
using System.Diagnostics;
using Interview;

namespace MultiplexingTests;

public class Tests

{
    private const int _asyncOperationMaxMilliseconds = 10;
    private const int _requestMaxMilliseconds = 20;

    public static async Task Main()
    {
        var processor = new ComplexRequestProcessor(
            new LowLevelNetworkAdapterMock(), TimeSpan.FromMilliseconds(_asyncOperationMaxMilliseconds));

        var cts = new CancellationTokenSource();
        _ = processor.StartAsync(cts.Token);

        new Thread(() => SendRequestsInLoop(processor, cts.Token)).Start();
        new Thread(() => SendRequestsInLoop(processor, cts.Token)).Start();

        await Task.Delay(10_000, cts.Token);

        new Thread(async () =>
        {
            await Task.Delay(1_000);
            cts.Cancel();
            cts.Dispose();
        }).Start();
        await processor.StopAsync(cts.Token);
        processor = null;
        Console.WriteLine("Stopped");
        GC.Collect(2);

        var gen0Count = GC.CollectionCount(0);
        var gen1Count = GC.CollectionCount(1);
        var gen2Count = GC.CollectionCount(2);

        Debugger.Break();
    }

    private static async Task SendRequestsInLoop(ComplexRequestProcessor _processor,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var newGuid = Guid.NewGuid();

            Console.WriteLine($"SEND    {Thread.CurrentThread.ManagedThreadId} {newGuid}");
            try
            {
                var response = await _processor.SendAsync(new Request(newGuid), cancellationToken);
                Console.WriteLine($"RECEIVE {Thread.CurrentThread.ManagedThreadId} {response.Id}");
            }
            catch (TimeoutException _)
            {
                Console.WriteLine($"TIMEOUT {Thread.CurrentThread.ManagedThreadId} {newGuid}");
            }
        }
    }

    public class LowLevelNetworkAdapterMock : ILowLevelNetworkAdapter
    {
        private readonly BlockingCollection<Request> blockingCollection = new(10);

        public async Task<Response> ReadAsync(CancellationToken token)
        {
            await Task.Delay(Random.Shared.Next() % _requestMaxMilliseconds, token);
            var item = blockingCollection.Take();
            return new Response(item.Id);
        }

        public async Task WriteAsync(Request request, CancellationToken token)
        {
            await Task.Delay(Random.Shared.Next() % _requestMaxMilliseconds, token);

            blockingCollection.Add(request, token);
            if (Random.Shared.Next() % 2 == 0)
                // simulate wrong behavior when server can complete request more then 1 times
                blockingCollection.Add(request, token);
        }
    }
}