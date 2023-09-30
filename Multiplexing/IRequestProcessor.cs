namespace Interview;

public interface IRequestProcessor
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<Response> SendAsync(Request request, CancellationToken cancellationToken);
}