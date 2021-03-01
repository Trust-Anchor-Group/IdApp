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

TAG has a custom light-weight `Inversion of Control` implementation for dependency resolution called `Types`.
The reason for this is that the built-in `DependencyService` is just a service locator, not a dependency injection container.
It is rather limited, therefore swapping it out for `Types` is recommended. In order to specify how types should be resolved you can use
two attributes:
1. DefaultImplementationAttribute - specifies which class is the default implementation of a certain interface.

**Example:**
```
[DefaultImplementation(typeof(ImageCacheService))]
public interface IImageCacheService : ILoadableService
```
2. SingletonAttribute - specifies that there should only be one shared instance of this implementation.

**Example:**
``` 
[Singleton]
internal sealed class ImageCacheService : LoadableService, IImageCacheService
```

The two attributes can be used in combination on an interface and its implementation.

If you need to register types to resolve, make that call to `Types.Initialize` _before_ the call to `Types.Instantiate<T>()`, like this:
```
Assembly appAssembly = this.GetType().Assembly;

if (!Types.IsInitialized)
{
    // Define the scope and reach of Runtime.Inventory (Script, Serialization, Persistence, IoC, etc.):
    Types.Initialize(
        appAssembly,                                // Allows for objects defined in this assembly, to be instantiated and persisted.
        typeof(Database).Assembly,                  // Indexes default attributes
        typeof(ObjectSerializer).Assembly,          // Indexes general serializers
        typeof(FilesProvider).Assembly,             // Indexes special serializers
        typeof(RuntimeSettings).Assembly,           // Allows for persistence of settings in the object database
        typeof(XmppClient).Assembly,                // Serialization of general XMPP objects
        typeof(ContractsClient).Assembly,           // Serialization of XMPP objects related to digital identities and smart contracts
        typeof(Expression).Assembly,                // Indexes basic script functions
        typeof(XmppServerlessMessaging).Assembly,   // Indexes End-to-End encryption mechanisms
        typeof(TagConfiguration).Assembly,          // Indexes persistable objects
        typeof(RegistrationStep).Assembly);         // Indexes persistable objects
}

```

Configure `Types` in the [App.xaml.cs](../IdApp/IdApp/App.xaml.cs) constructor to be the default resolver like this:

```
public App()
{
    ...

    // Set the IoC to be the default dependency resolver
    DependencyResolver.ResolveUsing(type =>
	{
        if (Types.GetType(type.FullName) is null)
	        return null;	// Type not managed by Runtime.Inventory. Xamarin.Forms resolves this using its default mechanism.
        return Types.Instantiate(true, type);
    });
}
```

That's all you need to do. And when you need to resolve components later in the code, invoke the built-in `DependencyService` as usual:

```
    var myService = DependencyService.Resolve<IMyServie>();
```
This will invoke the lightweight IoC implementation 'under the hood'.

## The TAG Neuron SDK Structure ##

The core, or root of the TAG Neuron SDK starts with the declared service interfaces. You create an instance of the services in your `App` constructor like this:

```
    this.neuronService = Types.InstantiateDefault<INeuronService>(false);
    ....
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
 
Use these overrides to hook into the TAG Neuron SDK (the services). Here's the way to do it:
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
    await this.neuronService.Load(isResuming);
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

    await this.neuronService.Unload();
    // Call or invoke other services here.
    ....
}

```
When this is done, you can start and run the application. It won't do anything, but now it's wired into the SDK correctly.

## Developing the App ##
The app is built with the industry standard [MVVM](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/enterprise-application-patterns/mvvm) pattern.
[Here's a short intro](Mvvm.md) with links to further reading.

## Debugging the App ##
You can debug the app in the Android Emulator, the iOS emulator, or on a real phone.

### Android specifics ###
The minimum Android version is 'Q' (API level 29), this is set in Application tab _and_ in the Android Manifest tag in the `IdApp.Android` properties.
In Visual Studio, click on "Tools"-> "Android" -> "Android Device Manager". If the list is empty, you need to create a new device. Click on the "New" button,
and make sure the "OS" dropdown in the bottom left corner is set to at least 'Q' (API level 29). When done, hit the "Create" button. This device can now
be used for running and debugging the app.

### iOS specifics ###
The minimum iOS version is 9 (change it by opening the `Info.plist` file in the `IdApp.iOS project`).
In order to run the iOS version of the app on either a simulator or on a real phone, you must have your [paired your Windows](https://docs.microsoft.com/en-us/xamarin/ios/get-started/installation/windows/connecting-to-mac/) machine to a Mac, i.e. the Mac host is available in the network.
Once the pairing is set up, hit F5 to debug in the Simulator or on your iPhone.

## Deploying the App ##
Deploying, as opposed to debugging, requires the app to be signed. This is done slightly differently for Android and iOS.

### Android specifics ###
In order to sign the app you need a Keystore. One can easily be created, [read on here for details](https://docs.microsoft.com/en-us/xamarin/android/deploy-test/signing/?tabs=windows).

Now open the Properties page for the IdApp.Android project and select the "Android Package Signing" tab. Tick the checkbox and fill in the values matching your keystore.

Right-click the IdApp.Android project and choose "Archive...". This will build an app archive, and then display the Archive Manager page in Visual Studio. Select the latest archive
and click on the "Distribute" button. Choose "Ad Hoc" for internal distribution, or "Google Play" for upload to the Play Store. Select the Keystore again, and hit ok. This will create
a signed version of the app, ready for distribution.

### iOS specifics ###
Signing happens 'automatically' when building the iOS version of the app, as it is associated with an Apple developer license. Right-click the IdApp.iOS project and choose "Properties", then the "iOS Bundle Signing" tab.
There you can specify how to provision the app. Either automatically or manually. When your app signing is set up correctly, the build process will produce a correctly signed
app for use on an iOS device. If you want to distribute it to a select group, or to the App Store, you may need a different provisioning profile than the one you use for development.

## Next Steps ##
For further reading, please continue to these sections:
- [The XMPP Protocol and Neuron](Xmpp.md)
- [The ID App](AppAnatomy.md)
- [Creating a TAG Profile](CreatingATAGProfile.md)
- [Neuron SDK](NeuronSDK.md)
- [Neuron SDK UI](NeuronSDKUI.md)
- [Branch Strategy](BranchStrategy.md)
- [Command Line Builds (for CI)](CommandLineBuild.md)
