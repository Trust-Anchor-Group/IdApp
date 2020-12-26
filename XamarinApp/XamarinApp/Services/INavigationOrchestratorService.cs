using Tag.Sdk.Core.Services;

namespace XamarinApp.Services
{
    /// <summary>
    /// Keeps track of the <see cref="TagProfile"/> for the current user, and applies the correct navigation should the legal identity be compromised or revoked.
    /// </summary>
    public interface INavigationOrchestratorService : ILoadableService
    {
    }
}