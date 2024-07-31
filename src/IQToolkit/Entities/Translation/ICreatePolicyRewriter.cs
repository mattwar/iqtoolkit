namespace IQToolkit.Entities.Translation
{
    public interface ICreatePolicyRewriter
    {
        QueryPolicyRewriter CreatePolicyTranslator(QueryTranslator translator);
    }
}
