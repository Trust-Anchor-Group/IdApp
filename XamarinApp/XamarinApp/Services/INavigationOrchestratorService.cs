using Tag.Neuron.Xamarin.Services;

namespace XamarinApp.Services
{
    /// <summary>
    /// Keeps track of the <see cref="ITagProfile"/> for the current user, and applies the correct navigation should the legal identity be compromised or revoked.
    /// </summary>
    public interface INavigationOrchestratorService : ILoadableService
    {
    }
}