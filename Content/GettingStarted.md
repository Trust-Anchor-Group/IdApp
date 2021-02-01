# Getting Started #
In order to build a Xamarin app that uses the Neuron server you need to include the TAG Neuron SDK. 

## Table of Contents ##
- [Getting Started](#getting-started)
- [Dependency Resolution](#dependency-resolution)
- [The TAG Neuron SDK Structure](#the-tag-neuron-sdk-structure)
- [Registration Keys](#registration-keys)
- [Creating the app](#creating-the-app)
- [Next Steps](#next-steps)

## Getting Started ##
The TAG Neuron SDK is easy to integrate into any Xamarin App via a few lines of code.

### Dependency resolution ###
TAG recommends using [AutoFac](https://autofac.org/) for `IoC` and dependency resolution. 
The reason for this is that the built-in `DependencyService` is just a service locator, not a dependency injection container.
It is rather limited, therefore swapping it out for AutoFac is recommended.

Add a reference
to the `AutoFac` Nuget, and then add the following using statement at the top of ```App.xaml.cs```:

```
    using Autofac;
```
Now create a container builder in the `App.xaml.cs` constructor like this:

```
public App()
{
    ContainerBuilder builder = new ContainerBuilder();

    // Register dependencies here
    ...

    // Build the container
    IContainer container = builder.Build();

    // Set AutoFac to be the default dependency resolver
    DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);
}
```
That's all you need to do. And when you need to resolve components later in the code, invoke the built-in `DependencyService` as usual:
```
    var myService = DependencyService.Resolve<IMyServie>();
```
This will invoke the AutoFac IoC under the hood.

## The TAG Neuron SDK Structure ##
The core, or root of the TAG Neuron SDK starts with the [`ITagIdSdk`](../Tag.Neuron.Xamarin/ITagIdSdk.cs) interface. 
Everything you need can be accessed via this interface. You create an instance of the SDK in your `App` constructor like this:
```
    this.sdk = TagIdSdk.Create(this, new Registration().ToArray());
```
Once this is done you can register the TAG Neuron SDK services with AutoFac like this (See details on [Registration Keys](#registration-keys) below)
```
public App()
{
    // Create the SDK
    this.sdk = TagIdSdk.Create(this.GetType().Assembly, null, new Registration().ToArray());

    ContainerBuilder builder = new ContainerBuilder();

    // Registrations
    builder.RegisterInstance(this.sdk.UiDispatcher).SingleInstance();
    builder.RegisterInstance(this.sdk.TagProfile).SingleInstance();
    builder.RegisterInstance(this.sdk.NeuronService).SingleInstance();
    builder.RegisterInstance(this.sdk.AuthService).SingleInstance();
    builder.RegisterInstance(this.sdk.NetworkService).SingleInstance();
    builder.RegisterInstance(this.sdk.LogService).SingleInstance();
    builder.RegisterInstance(this.sdk.StorageService).SingleInstance();
    builder.RegisterInstance(this.sdk.SettingsService).SingleInstance();
    builder.RegisterInstance(this.sdk.NavigationService).SingleInstance();

    // Add your own registrations here
    ...
    // Build the container
    IContainer container = builder.Build();

    // Set AutoFac to be the default dependency resolver
    DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);
}
```
For further reading about the TAG Neuron SDK, [have a look here](NeuronSDK.md).

## Registration Keys ##
Registration keys for each Neuron domain is specified separately in a partial class, see [`Registration.cs`](../IdApp/IdApp/Services/Registration.cs).
As the class is partial, the _other_ 'half' must reside in the folder _above_ the solution folder.
If you have the following folder structure:

```/Path/To/Solution/IdApp/```

Then the `IdApp.sln` file is located in that folder. Run the build once, this will copy the Registration.cs file (via a custom build step) to folder _above_ the solution folder. 
In this case it would be the `/Path/To/Solution/` folder.
Edit _this_ file, providing domain names and cryptographic keys.

#### Motivation ####
Cryptographic keys or secrets should never be added to source control. By placing this partial class declaration on your hard drive, but _outside_ the regular solution and folder structure,
the keys can be kept secret, but copied in temporarily during compilation, and then removed immediately again. *Hence the custom build steps*.

## Creating the app ##
1. Start by creating a standard Xamarin App for iOS and Android.
2. Open up the `App.xaml.cs` file in the editor.
3. Apply the code changes in the `App` constructor as suggested [above](#the-tag-neuron-sdk-structure).
4. Add `override`s for the following methods:
    1. `OnStart()`
    2. `OnResume()`
    3. `OnSleep()`
 
Use these overrides to hook into the TAG Neuron SDK. Here's the way to do it:
```
protected override async void OnStart()
{
    await PerformStartup(false);
}

protected override async void OnResume()
{
    await PerformStartup(true);
}

protected override async void OnSleep()
{
    await PerformShutdown();
}
```
5. Now add the implementation in the following two methods, and you're done.
```
private async Task PerformStartup(bool isResuming)
{
    await this.sdk.Startup(isResuming);
    // Call or invoke other services here.
    ....
}

private async Task PerformShutdown()
{
    // Done manually here, as the Disappearing event won't trigger when exiting the app,
    // and we want to make sure state is persisted and teardown is done correctly to avoid memory leaks.
    if (MainPage?.BindingContext is BaseViewModel vm)
    {
        await vm.SaveState();
        await vm.Unbind();
    }

    await this.sdk.Shutdown(keepRunningInTheBackground);
}

```
When this is done, you can start and run the application. It won't do anything, but now it's wired into the SDK correctly.

## Next Steps ##
For further reading, please continue to these sections:

- [The ID App](AppAnatomy.md)
- [Neuron Registration](NeuronRegistration.md)
- [Neuron SDK](NeuronSDK.md)
- [Neuron SDK UI](NeuronSDKUI.md)
