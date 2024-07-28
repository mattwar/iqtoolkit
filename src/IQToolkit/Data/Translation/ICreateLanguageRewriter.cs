namespace IQToolkit.Data.Translation
{
    internal interface ICreateLanguageRewriter
    {
        QueryLanguageRewriter CreateLanguageTranslator(QueryTranslator translator);
    }
}
