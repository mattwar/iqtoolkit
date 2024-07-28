namespace IQToolkit.Data.Translation
{
    public interface ICreatePolicyTranslator
    {
        QueryPolicyRewriter CreatePolicyTranslator(QueryTranslator translator);
    }
}
