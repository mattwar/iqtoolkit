namespace IQToolkit.Entities.Translation
{
    internal interface ICreateLanguageRewriter
    {
        QueryLanguageRewriter CreateLanguageTranslator(QueryTranslator translator);
    }
}
