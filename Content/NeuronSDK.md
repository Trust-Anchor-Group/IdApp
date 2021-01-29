# TAG Neuron SDK #
The TAG Neuron SDK is built to easily integrate the [IoTGateway](https://github.com/PeterWaher/IoTGateway) framework into a Xamarin app.
It all starts with the [`ITagIdSdk`](../Tag.Neuron.Xamarin/ITagIdSdk.cs) interface.

You create an instance of the SDK via the static `Create()` factory method on the [`TagIdSdk`](../Tag.Neuron.Xamarin/TagIdSdk.cs) class.
Pass in the app assembly and the registration keys for each domain. You can read more about the [registration keys here](GettingStarted.md#registration-keys).

```
public partial class App
{
    private readonly ITagIdSdk sdk;

    public App()
    {
        this.sdk = TagIdSdk.Create(this, new Registration().ToArray());
    }
}
```
Once this is done you have access to all features and services the SDK provides. You can use it as-is, or register the services via the IoC Container of your choosing.
Many of the services are wafer-thin wrappers around platform features, making it very easy to mock for unit testing.

## Features/Services ##
Here's a list of all services the SDK provides, and what you can use them for.

### TagProfile ###
The [TagProfile](../Tag.Neuron.Xamarin/Services/ITagProfile.cs) is the user profile, which holds data about a user's identity, as well as Jabber Ids for the services discovered for the domain the user is connected to.
This is a very central class, and the user profile can be built/completed in several steps, adding more information for each step. This is done during the registration phase,
[read more about it here](NeuronRegistration.md).

### UiDispatcher ###
The [UiDispatcher](../Tag.Neuron.Xamarin/IUiDispatcher.cs) provides access to the main thread. The `UiDispatcher` is mainly used for two things:
1. Doing a `BeginInvoke` (i.e. marshalling) on the main thread, and
2. Displaying alerts to the user.

### NeuronService ###
The [NeuronService](../Tag.Neuron.Xamarin/Services/INeuronService.cs) represents the live connection to a Neuron server.
It provides event handlers for managing connected state, as well as methods to access the various services the Neuron server provides (Contract handling, Chats et.c)

### AuthService ###
The [AuthService](../Tag.Neuron.Xamarin/Services/IAuthService.cs) has methods to help with cryptographic tasks like creating a random password et.c.

### NetworkService ###
The [NetworkService](../Tag.Neuron.Xamarin/Services/INetworkService.cs) allows you to check for network access, and it also provides helper methods
to make arbitrary requests with consise error handling built-in. That's what all the `TryRequest` methods are for.
If any call fails, they will catch errors and display alerts to the user. You don't have to use these, but they are provided for convenience.




