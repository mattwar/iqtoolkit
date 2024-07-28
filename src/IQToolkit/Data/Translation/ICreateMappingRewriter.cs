namespace IQToolkit.Data.Translation
{
    public interface ICreateMappingRewriter
    {
        QueryMappingRewriter CreateMappingTranslator(QueryTranslator translator);
    }
}
