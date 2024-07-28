namespace IQToolkit.Data.Translation
{
    public interface ICreatePolicyRewriter
    {
        QueryPolicyRewriter CreatePolicyTranslator(QueryTranslator translator);
    }
}
