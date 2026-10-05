namespace Starter.Shared.Output;

// result tools print data through one writer per output mode
public interface IResultWriter<in T>
{
    void Write(T result, TimeSpan elapsed);
}
