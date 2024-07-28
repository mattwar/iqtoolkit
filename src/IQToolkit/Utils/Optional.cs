namespace IQToolkit.Utils
{
    public struct Optional<TValue>
    {
        public bool HasValue { get; }
        public TValue Value { get; }

        private Optional(bool hasValue, TValue value)
        {
            this.HasValue = hasValue;
            this.Value = value;
        }

        public Optional(TValue value)
            : this(true, value)
        {
        }

        public static Optional<TValue> None =
            new Optional<TValue>(false, default!);

        public static implicit operator Optional<TValue>(TValue value) =>
            new Optional<TValue>(value);
    }
}