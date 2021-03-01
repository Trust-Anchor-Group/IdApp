# TAG Neuron SDK #
The TAG Neuron SDK is built to easily integrate the [IoTGateway](https://github.com/PeterWaher/IoTGateway) framework into a Xamarin app.
It is wrapped using a list of interfaces, providing various services.

You create an instance of the services you need in the App constructor. Pass in the registration keys for each domain to the `ITagProfile` instance. You can read more about the [registration keys here](GettingStarted.md#registration-keys).

```
public partial class App
{
    private readonly ITagProfile tagProfile;
    private readonly ILogService logService;
    private readonly INeuronService neuronService;
    ...

    public App()
    {
        this.tagProfile = Types.InstantiateDefault<TagProfile>(false, (object)new XmppConfiguration().ToArray());
        this.logService = Types.InstantiateDefault<ILogService>();
        this.neuronService = Types.InstantiateDefault<INeuronService>();
        ....
    }
}
```
Once this is done you have access to the features and services the SDK provides. You can use it as-is, or register the services via the IoC Container of your choosing.
Many of the services are wafer-thin wrappers around platform features, making it very easy to mock for unit testing.

## Features/Services ##
Here's a list of all services the SDK provides, and what you can use them for.

### TagProfile ###
The [TagProfile](../Tag.Neuron.Xamarin/Services/ITagProfile.cs) is the user profile, which holds data about a user's identity, as well as Jabber Ids for the services discovered for the domain the user is connected to.
This is a very central class, and the user profile can be built/completed in several steps, adding more information for each step. This is done during the registration phase,
[read more about it here](CreatingATAGProfile.md).

### UiDispatcher ###
The [UiDispatcher](../Tag.Neuron.Xamarin/IUiDispatcher.cs) provides access to the main thread. The `UiDispatcher` is mainly used for two things:
1. Doing a `BeginInvoke` (i.e. marshalling) on the main thread, and
2. Displaying alerts to the user.
It makes it easy to mock calls for unit testing, but using it is completely optional.

### NeuronService ###
The [NeuronService](../Tag.Neuron.Xamarin/Services/INeuronService.cs) represents the live connection to a Neuron server.
It provides event handlers for managing connected state, as well as methods to access the various services the Neuron server provides (Contract handling, Chats et.c)

### CryptoService ###
The [CryptoService](../Tag.Neuron.Xamarin/Services/ICryptoService.cs) has methods to help with cryptographic tasks like creating a random password et.c.

### NetworkService ###
The [NetworkService](../Tag.Neuron.Xamarin/Services/INetworkService.cs) allows you to check for network access, and it also provides helper methods
to make arbitrary requests with consise error handling built-in. That's what the `TryRequest` methods are for.
If any call fails, they will catch errors, log them and display alerts to the user. You don't have to use these, but they are provided for convenience.

### LogService ###
The [LogService](../Tag.Neuron.Xamarin/Services/ILogService.cs) allows you to save log statements which are then reported back to the Neuron server.
You can of course also attach `IEventSink`s to subscribe to log events, and redirect them to other
services like [Microsoft App Center](https://appcenter.ms/apps) for tracking Crashes and Analytics. The [AppCenterEventSink](../IdApp/IdApp/AppCenterEventSink.cs)
is an example of this. You can see it being added in the [App.xaml.cs](../IdApp/IdApp/App.xaml.cs) constructor.

### StorageService ###
The [StorageService](../Tag.Neuron.Xamarin/Services/IStorageService.cs) represents persistent storage, i.e. a Database. The content is encrypted. In order to
store any object in the database, the type of object to store must be made known to the SDK. This is what the parameters to `Types.Initialize` in the App constructor is for.

Pass in one or more assemblies that contain the type(s) you need stored in the database.
The type you want stored should have a collection name. Set one using an attribute like this:
```
[CollectionName("Orders")]
public sealed class CustomerOrders
{
}
```
Once that is done, add an `Id` property and specify the attribute as follows:
```
[CollectionName("Orders")]
public sealed class CustomerOrders
{
    [ObjectId]
    public string ObjectId { get; set; }
}
```
This will be the primary key for the object in the database. No need to set it, just declare it like this.

### SettingsService ###
The [SettingsService](../Tag.Neuron.Xamarin/Services/ISettingsService.cs) is for storing user specific settings, like what they last typed into an `Entry` field or similar.
It is typically used for loading and saving UI state in the view models. The `Tag.Neuron.Xamarin.UI` library's [BaseViewModel](../Tag.Neuron.Xamarin.UI/ViewModels/BaseViewModel.cs) class has a helper method for this callled `GetSettingsKey`:
```
this.SettingsService.SaveState(GetSettingsKey(nameof(FirstName)), this.FirstName);

```
It's easy to work with, and as you can see, refactor friendly as it doesn't use string literals anywhere.

### NavigationService ###
The [NavigationService](../Tag.Neuron.Xamarin/Services/INavigationService.cs) is for navigating to various pages in the app. It is a simple wrapper around the `Shell.Current.GoToAsync()` methods,
but adds live parameter handling also. It means that instead of passing parameters via the route, you can pass real objects from one page to another. See subclasses of [`NavigationArgs`](../Tag.Neuron.Xamarin/Services/NavigationArgs.cs) for details.

Here's a real world example:
```
await this.navigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(identity, null));

```
The main purpose of the `NaviagationService` is two-fold:
1. Provide an easily mockable navigation interface for testing
2. Make it easy to find all navigation calls in the code, as well as pass live parameters.

Using it is completely optional, feel free to call `Shell.Current.GoToAsync()` directly instead.

### A Note on Dispose ###
In general, Dispose is used to handle _unmanaged_ resources. This is usually not needed, but _can_ of course be implemented
in various parts of the app. However, it should _not_ be done in the `Service` instances, as these are **re-used** during restarts.
When the app shuts down, all the services' `Unload()` method is called. When the app starts, the services' `Load()` method is called.
This is especially important to know during soft restarts, like when you're switching apps. In this case the pages and view models are kept around,
which means they reference the same services. If you dispose a service and then recreate it, the app will fail.