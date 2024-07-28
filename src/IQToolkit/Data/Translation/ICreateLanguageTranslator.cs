namespace IQToolkit.Data.Translation
{
    internal interface ICreateLanguageTranslator
    {
        QueryLanguageRewriter CreateLanguageTranslator(QueryTranslator translator);
    }
}
