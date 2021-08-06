# User Interface #

The TAG ID App uses standard Xamarin features, and is built around the [Xamarin Shell](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/shell/) experience.

## Overview ##

The root of the UI is of course the [`AppShell`](../IdApp/IdApp/AppShell.xaml) implementation. It declares the UI's visual hierarchy.
As you can see it is quite simple. It consists (in order) of a [`LoadingPage`](../IdApp/IdApp/Views/LoadingPage.xaml) which is displayed on app startup.
It is then followed by the [`MainPage`](../IdApp/IdApp/Views/MainPage.xaml) which is what the user normally sees when the app has started.
From there the user can navigate to various sub-pages using the menu items.

### Routing ###

In order for navigation to work routes must be registrered. This is done in the [`AppShell.xaml.cs`](../IdApp/IdApp/AppShell.xaml.cs) file, in the `RegisterRoutes` method.
If you want to add new pages to the app you can either add them as [Flyout](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/shell/flyout) items declaratively
in the app shell, or as menu items. It depends on how you want the navigation to work. The purpose of using `MenuItem`s in the ID App is to allow the user to go back
via the back arrow button to the `MainPage`.

## App Hierarchy ##

Looking at the anatomy of an app from top to bottom, this is the general structure:
```
    Pages/Views
        |
    ViewModels
        |
    Services	
```

The higher up in the 'food chain' you are, the more short-lived the objects are. 

Pages and Views are created and discarded at will.

The ViewModels typically have a slightly longer lifetime, but not much.

The Services typically live for as long as the current session lasts.

The purpose and big advantaged of [MVVM](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93viewmodel) is to be able to have a loose binding between views and viewmodels, but also keep state and business logic in the viewmodels. 
This allows for better readability, testability to name a few reasons.

## Pages and ViewModels ##

Traditionally `xaml` is built using the [MVVM](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93viewmodel) paradigm, and this is the case here also.
Every page or view has a corresponding `ViewModel` to which it is bound. The page or view represents the UI, and the view model holds state and business logic.

### Binding ###

In order to predictively be able to load and discard data, event handlers et.c. there are some built-in events we can utilize.
Every Xamarin `Page`  as two events that are of specific interest:

 - Appearing
 - Disappearing

Those are fired just before the `Page` is rendered on screen, and just before it disappears from the screen.
This can be utilized so do deterministic setup/teardown in our viewmodels. This what the 
[`ContentBasePage`](../IdApp.UI/Views/ContentBasePage.cs) is for.
That class, together with the [`BaseViewModel`](../IdApp.UI/ViewModels/BaseViewModel.cs) is what everything work automagically.

## Implementing a Page ##

Implementing a new page is easy. You can create arbitrary pages, and arbitrary viewmodels.

1. Create a random Xaml `Page`.
2. Have it subclass the [`ContentBasePage`](../IdApp.UI/Views/ContentBasePage.cs).
3. Create a matching `ViewModel`, and let that viewmodel subclass [`BaseViewModel`](../IdApp.UI/ViewModels/BaseViewModel.cs).
4. Assign the `ViewModel` to the page's binding context in the page constructor like this:

```
public partial class MyPage
{
    public MyPage()
    {
        ViewModel = new MyViewModel();
    }
}
```

By doing this you have now associated the `ViewModel` with the `Page`. It means that whenever the `Page` is displayed, the `ViewModel`s corresponding `Bind`/`Unbind` methods will be called,
as well as the `SaveState`/`RestoreState` methods.

5. Register the page's route (see the section [routing](#routing) above).

## A Note on ViewModels ##

Viewmodels are inherently UI specific. They serve the UI, and they operate mostly in UI land, i.e. on the main thread. For this reason, and to make xaml binding as easy
and smooth as possible, they inherit from the [`BindableObject`](https://docs.microsoft.com/en-us/dotnet/api/xamarin.forms.bindableobject?view=xamarin-forms).
This makes it super-easy to create Bindable properties, which is neccessary to get bindings to [Behaviors](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/behaviors/), animations and the [Visual State Manager](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/visual-state-manager) to work.
Just becaue they are UI centric doesn't mean you can execute code on a background thread. Just make sure you marshal back to the main thread when done. Use the [`IUiDispatcher`](../IdApp/IUiDispatcher.cs) for this.

## Adding Business Logic ##

This is done fully in the `ViewModel`. Start by resolving any dependencies you might need in the constructor:

```
public class MyViewModel : BaseViewModel
{
    private readonly INeuronService neuronService;

    public MyViewModel()
    {
        neuronService = DependencyService.Resolve<INeuronService>();
    }
}
```

Now continue adding code for setup/teardown by overriding the `DoBind` and `DoUnbind` methods respectively (**note** the order in things are done):

```
public class MyViewModel : BaseViewModel
{
    protected override async Task DoBind()
    {
        await base.DoBind();
        neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
    }

    protected override async Task DoUnind()
    {
        neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
        await base.DoUnind();
    }

    private void NeuronService_ConnectionStateChanged(object sender, ConnectivityChangedEventArgs e)
    {
        // Do stuff
    }
}
```
