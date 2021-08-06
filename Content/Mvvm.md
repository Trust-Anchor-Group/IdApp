# MVVM #

[MVVM](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/enterprise-application-patterns/mvvm) stands for Model/View/ViewModel, 
and is _the_ paradigm to use for xaml-based applications. The major benefits are:

- Loosely coupled application
- Very testable business logic

## Loose coupling ##

The loose coupling is related to how the UI is bound to the business logic.
In this case the UI is represented by pages and views, the **V** in M**V**VM.
The business logic is represented by view models (the **VM** in MV**VM**), holding data (The **M** in **M**VVM) and state.

### Binding ###

Binding is done in xaml, just like it's done in WPF. A view can be bound to
any object, and as long as that object has a property with the same name as the view binds to,
it will work. The view model can be any POCO class, but for convenience it typically inherits
from a [BindableObject](https://docs.microsoft.com/en-us/dotnet/api/xamarin.forms.bindableobject?view=xamarin-forms). The convenience
of binding to `BindableObject` is that change management (property change notifications) are handled automatically.

### Models ###

Models are the raw data, and it too can be any POCO class.

## Business Logic ##

The core business logic goes into the view models. Since they are agnostic to
both the UI (the Pages and Views), it knows nothing about them, and also the underlying network connections and persistent storage,
it makes them a prime candiate for extensive unit testing.

Supply their dependencies in the view model constructor, and start writing code.

## Id App specifics ##

To help implement the MVVM Pattern there are a few very lightweight classes to get started.

### Views ###

Any page should inherit from the [`ContentBasePage`](../IdApp.UI/Views/ContentBasePage.cs).
By inheriting from this class you get some convenience properties to access the view model it is bound to et.c.

### ViewModels ###

Any view model should inherit from the [`BaseViewModel`](../IdApp.UI/ViewModels/BaseViewModel.cs). This base class
provides methods to override when its corresponding view appears on screen and disappears from screen respectively.

The relevant methods are:

- `DoBind()` - for setup like adding event handlers et.c.
- `DoUnBind()` - for teardown like removing event handlers et.c.
- `DoSaveState()` - for persisting any UI state before a page/view is closed
- `DoRestoreState()` - for loading any UI state before a page/view is shown

The `BaseViewModel` class also supports adding child view models, often used in Master/Detail views for example.

## Next steps/further reading ##

Microsoft has [extensive documentation](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/data-binding/) on 
binding that is well worth reading.