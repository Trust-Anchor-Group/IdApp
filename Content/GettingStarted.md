# Getting Started #

In order to build a Xamarin app that uses the Neuron server you need to include the TAG Neuron SDK. But first, have a look at how 
TAG has integrated XMPP into a [`Neuron` server](Xmpp.md).

## Table of Contents ##

- [Introduction](#introduction)
- [Dependency Resolution](#dependency-resolution)
- [The TAG Neuron SDK Structure](#the-tag-neuron-sdk-structure)
- [Registration Keys](#registration-keys)
- [Creating the app](#creating-the-app)
- [Next Steps](#next-steps)

## Introduction ##

The TAG Neuron SDK is easy to integrate into any Xamarin App via a few lines of code.

### Dependency resolution ###

TAG  has a custom light-weight [`IoCContainer`](../Tag.Neuron.Xamarin/IoCContainer.cs) implementation for dependency resolution. 
The reason for this is that the built-in `DependencyService` is just a service locator, not a dependency injection container.
It is rather limited, therefore swapping it out for the `IoCContainer` is recommended.

Create an `IoCContainer` instance in the [App.xaml.cs](../IdApp/IdApp/App.xaml.cs) constructor like this:

```
public App()
{
    container = new IoCContainer();

    // Register dependencies here
    ...

    // Set the IoC to be the default dependency resolver
    DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);
}
```

That's all you need to do. And when you need to resolve components later in the code, invoke the built-in `DependencyService` as usual:

```
    var myService = DependencyService.Resolve<IMyServie>();
```
This will invoke the lightweight IoC implementation 'under the hood'. _**Remember**_ to `Dispose()` it later.

## The TAG Neuron SDK Structure ##

The core, or root of the TAG Neuron SDK starts with the [`ITagIdSdk`](../Tag.Neuron.Xamarin/ITagIdSdk.cs) interface. 
Everything you need can be accessed via this interface. You create an instance of the SDK in your `App` constructor like this:

```
    this.sdk = TagIdSdk.Create(this, new Registration().ToArray());
```

Once this is done you can register the TAG Neuron SDK services with an IoC container like this (See details on [Registration Keys](#registration-keys-for-developers) below)

```
public App()
{
    // Create the SDK
    this.sdk = TagIdSdk.Create(this.GetType().Assembly, null, new Registration().ToArray());

    this.container = new IoCContainer();

    // Registrations
    this.container.RegisterInstance(this.sdk.UiDispatcher).SingleInstance();
    this.container.RegisterInstance(this.sdk.TagProfile).SingleInstance();
    this.container.RegisterInstance(this.sdk.NeuronService).SingleInstance();
    this.container.RegisterInstance(this.sdk.AuthService).SingleInstance();
    this.container.RegisterInstance(this.sdk.NetworkService).SingleInstance();
    this.container.RegisterInstance(this.sdk.LogService).SingleInstance();
    this.container.RegisterInstance(this.sdk.StorageService).SingleInstance();
    this.container.RegisterInstance(this.sdk.SettingsService).SingleInstance();
    this.container.RegisterInstance(this.sdk.NavigationService).SingleInstance();

    // Add your own registrations here
    ...

    // Set resolver
    DependencyResolver.ResolveUsing(type => this.container.IsRegistered(type) ? this.container.Resolve(type) : null);
}
```
For further reading about the TAG Neuron SDK, [have a look here](NeuronSDK.md).

## Registration Keys (for developers) ##
Registration keys for each Neuron domain is specified separately in a partial class, see [`Registration.cs`](../IdApp/IdApp/Services/Registration.cs).
As the class is partial, the _other_ 'half' must reside in the folder _above_ the solution folder. The reason for this is that the solution uses a custom build step.
If you have the following folder structure:

```/Path/To/Solution/IdApp/```

Then the `IdApp.sln` file is located in that folder.
### First time setup ###
Build the solution once. This will trigger the custom build step which will copy the `Registration.cs` file to folder _above_ the solution folder. 
In this case it would be the `/Path/To/Solution/` folder.
Edit _this_ file, providing domain names and cryptographic keys. Here's _exactly_ what is should look like, just edit the three values:
```
using System;
using System.Collections.Generic;

namespace Waher.IoTGateway.Setup
{
  public partial class XmppConfiguration
  {
      private readonly Dictionary<string, KeyValuePair<string, string>> clp = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.CurrentCultureIgnoreCase)
      {
        { "<domain>", new KeyValuePair<string, string>("<key>", "<secret>") }};
      }
  }
}
``` 

#### Motivation ####
Cryptographic keys or secrets should _never_ be added to source control. By placing this partial class declaration on your hard drive, but _outside_ the regular solution and folder structure,
the keys can be kept secret, but copied in temporarily during compilation, and then removed immediately again. *Hence the custom build steps*. This means the cryptographic keys
are never committed to source control, and only included in the compiled binaries.

### Acquiring Keys ###
Keys are necessary if using TAG Neurons, and they can be requested by the operator of the Neuron(s). The keys must:
- Have a registered owner
- Have a maximum number of accounts that can be created with the key

Accounts created using the specific key are linked to that key.

**If the keys are lost:**
Malicious users can create accounts up to, but not exceeding the limit of accounts set for the key. Maliciously created accounts can later be removed by the operator, if deemed necessary.

## Creating the app ##
1. Start by creating a standard Xamarin App for iOS and Android.
2. Open up the [App.xaml.cs](../IdApp/IdApp/App.xaml.cs) file in the editor.
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
        await vm.Shutdown();
    }

    await this.sdk.Shutdown(keepRunningInTheBackground);
}

```
When this is done, you can start and run the application. It won't do anything, but now it's wired into the SDK correctly.

## Next Steps ##
For further reading, please continue to these sections:
- [The XMPP Protocol and Neuron](Xmpp.md)
- [The ID App](AppAnatomy.md)
- [Creating a TAG Profile](CreatingATAGProfile.md)
- [Neuron SDK](NeuronSDK.md)
- [Neuron SDK UI](NeuronSDKUI.md)
- [Branch Strategy](BranchStrategy.md)
- [Command Line Builds (for CI)](CommandLineBuild.md)
