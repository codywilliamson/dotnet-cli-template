using Starter.Shared;
using Starter.Shared.Output;

namespace Starter.Features.Doctor;

// runs checks as reporter steps; a failure prints its fix and the rest still run
public sealed class Checklist(IReporter reporter)
{
    public int Problems { get; private set; }

    public async Task CheckAsync(string name, Func<StepScope, Task> work)
    {
        try
        {
            await reporter.StepAsync(name, work);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Problems++;
            reporter.Error(e.Message, (e as PrefixException)?.Hint);
        }
    }
}
