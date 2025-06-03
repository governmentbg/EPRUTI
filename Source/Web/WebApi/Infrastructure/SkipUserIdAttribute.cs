namespace WebApi.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SkipUserIdAttribute : Attribute
    {
    }
}
