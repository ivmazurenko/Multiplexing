namespace Interview;

public interface ILowLevelNetworkAdapter
{
    Task<Response> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(Request request, CancellationToken cancellationToken);
}