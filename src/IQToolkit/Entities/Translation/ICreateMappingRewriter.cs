namespace IQToolkit.Entities.Translation
{
    public interface ICreateMappingRewriter
    {
        QueryMappingRewriter CreateMappingTranslator(QueryTranslator translator);
    }
}
