namespace IQToolkit.Data
{
    /// <summary>
    /// A type that exposes a <see cref="Executor"/> property.
    /// </summary>
    public interface IHaveExecutor
    {
        QueryExecutor Executor { get; }
    }
}
