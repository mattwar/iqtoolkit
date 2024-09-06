namespace IQToolkit.Entities.Mapping
{
    using Utils;


    public static class MappingOptions
    {
        /// <summary>
        /// Infer mapped column members from the properties on the entity type.
        /// A entity type property without mapping information will be mapped to a column with the same name.
        /// </summary>
        public static readonly Option<bool> InferColumnMembersFromEntityType =
            new Option<bool>(nameof(InferColumnMembersFromEntityType), true);

        /// <summary>
        /// Infer existence of columns from column names referenced in column member mapping.
        /// </summary>
        public static readonly Option<bool> InferColumnsFromMembers =
            new Option<bool>(nameof(InferColumnsFromMembers), true);

        /// <summary>
        /// Infer existence of columns from column names referenced in association mapping.
        /// </summary>
        public static readonly Option<bool> InferColumnsFromAssociations =
            new Option<bool>(nameof(InferColumnsFromAssociations), true);
    }

    public static class MappingOptionExtensions
    {
        /// <summary>
        /// Infer existence of columns from column names referenced in column member mapping.
        /// </summary>
        public static Options WithInferColumnsFromMembers(this Options options, bool enable) =>
            options.WithOption(MappingOptions.InferColumnsFromMembers, enable);

        /// <summary>
        /// Infer existence of columns from column names referenced in column member mapping.
        /// </summary>
        public static bool InferColumnsFromMembers(this Options options) =>
            options.GetOption(MappingOptions.InferColumnsFromMembers);


    }
}
