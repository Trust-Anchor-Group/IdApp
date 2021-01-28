# Documentation #

In order to build a Xamarin app that uses the Neuron server you need to include the TAG Neuron SDK. 

## Getting Started ##

The TAG Neuron SDK is easy to integrate into any Xamarin App via a few lines of code.

### Dependency resolution ###
TAG recommends using [AutoFac](https://autofac.org/) for ```IoC``` and dependency resolution. 
The reason for this is that the built-in ```DependencyService``` is just a service locator, not a dependency injection container.
It is rather limited, therefore swapping it out for AutoFac is recommended.

Add a reference
to the ```AutoFac``` Nuget, and then add the following using statement at the top of ```App.xaml.cs```:

```
	using Autofac;
```
Now create a container builder in the ```App.xaml.cs``` constructor like this:

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
That's all you need to do. And when you need to resolve components later in the code, invoke the DependencyService as usual:
```
	var myService = DependencyService.Resolve<IMyServie>();
```

## Creating an app ##

1. Start by creating a standard Xamarin App for iOS and Android.
2. Open up the ```App.xaml.cs``` file in the editor.
3. 