using System.Runtime.ExceptionServices;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Disposes every item even when an earlier one throws, so a failing service cannot leak the others.
/// </summary>
internal static class DisposalHelper
{
    public static void DisposeAll(IEnumerable<IDisposable> disposables)
    {
        List<Exception>? exceptions = null;
        foreach (var disposable in disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        Rethrow(exceptions);
    }

    public static async ValueTask DisposeAllAsync(IEnumerable<IDisposable> disposables)
    {
        List<Exception>? exceptions = null;
        foreach (var disposable in disposables)
        {
            try
            {
                if (disposable is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        Rethrow(exceptions);
    }

    private static void Rethrow(List<Exception>? exceptions)
    {
        if (exceptions is null)
        {
            return;
        }

        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        throw new AggregateException(exceptions);
    }
}
