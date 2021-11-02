namespace Blogifier.Core.Providers
{
    public interface ICurrentUserProvider
    {
        string UserName { get; }
    }
}
