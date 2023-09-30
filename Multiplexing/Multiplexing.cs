namespace Interview;

/*
 * Наше приложение общается с удаленным сервисом: шлет запросы и получает ответы. С удаленным сервером
 * установлено единственное соединение, по которому мы шлем запросы и получаем ответы. Каждый запрос содержит Id (GUID),
 * ответ на запрос содержит его же. Ответы на запросы могут приходить в произвольном порядке и с произвольными задержками.
 * Сервер может иметь ошибки в реализации и отправить один ответ несколько раз.
 * Нам необходимо реализовать интерфейс, который абстрагирует факт такого мультиплексирования.
 * Реализация `IRequestProcessor.SendAsync` обязана быть потокобезопасной.
 *
 * У нас есть готовая реализация интерфейсов `ILowLevelNetworkAdapter` и `IHighLevelNetworkAdapter`
 */

// запрос, остальные поля не интересны
public sealed record Request(Guid Id);

// ответ, остальные поля не интересны
public sealed record Response(Guid Id);

// низкоуровневый адаптер, можно делать одновременный вызов ReadAsync и WriteAsync
// можно считать это абстракцией над полнодуплексным сокетом
// ----
// важное предположение о реализации: низкоуровневый сетевой интерфейс, который сделает реконнект,
// но при отмене Read/Write скорее всего просто оборвет текущее активное соединение и все незавершенные
// запросы будут потеряны
public interface ILowLevelNetworkAdapter
{
    // вычитывает очередной ответ, нельзя делать несколько одновременных вызовов ReadAsync
    Task<Response> ReadAsync(CancellationToken cancellationToken);

    // отправляет запрос, нельзя делать несколько одновременных вызовов WriteAsync
    Task WriteAsync(Request request, CancellationToken cancellationToken);
}

// интерфейс, который надо реализовать
public interface IRequestProcessor
{
    // Запускает обработчик, возвращаемый Task завершается после окончания инициализации
    // гарантированно вызывается 1 раз при инициализации приложения
    Task StartAsync(CancellationToken cancellationToken);

    // выполняет мягкую остановку, т.е. завершается после завершения обработки всех запросов
    // гарантированно вызывается 1 раз при остановке приложения
    Task StopAsync(CancellationToken cancellationToken);

    // выполняет запрос, этот метод будет вызываться в приложении множеством потоков одновременно
    // При отмене CancellationToken не обязательно гарантировать то, что мы не отправим запрос на сервер, но клиент должен получить отмену задачи
    Task<Response> SendAsync(Request request, CancellationToken cancellationToken);
}

// сложный вариант задачи:
// 1. можно пользоваться только ILowLevelNetworkAdapter
// 2. нужно реализовать обработку cancellationToken
// 3. нужно реализовать StopAsync, который дожидается получения ответов на уже переданные
//    запросы (пока не отменен переданный в `StopAsync` `CancellationToken`)
// 4. нужно реализовать настраиваемый таймаут: если ответ на конкретный запрос не получен за заданный промежуток
//    времени - отменяем задачу, которая вернулась из `SendAsync`. В том числе надо рассмотреть ситуацию,
//    что ответ на запрос не придет никогда, глобальный таймаут при этом должен отработать и не допустить утечки памяти
public sealed class ComplexRequestProcessor : IRequestProcessor
{
    private readonly ILowLevelNetworkAdapter _networkAdapter;
    private readonly TimeSpan _requestTimeout;

    public ComplexRequestProcessor(ILowLevelNetworkAdapter networkAdapter, TimeSpan requestTimeout)
    {
        if (requestTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));

        _networkAdapter = networkAdapter;
        _requestTimeout = requestTimeout;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Response> SendAsync(Request request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}