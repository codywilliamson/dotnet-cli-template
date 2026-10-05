using System.Text;
using Starter.Cli;

// redirected output on windows otherwise falls back to the oem code page
Console.OutputEncoding = new UTF8Encoding(false);

// first ctrl+c cancels and lets the current step finish; the second one kills the process
using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    if (cancel.IsCancellationRequested)
    {
        return;
    }

    e.Cancel = true;
    cancel.Cancel();
};

return (int)await new PrefixApp(CliEnvironment.FromProcess(), cancel.Token).RunAsync(args);
