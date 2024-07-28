namespace IQToolkit.Data.Translation
{
    public interface ICreateMappingTranslator
    {
        QueryMappingRewriter CreateMappingTranslator(QueryTranslator translator);
    }
}
