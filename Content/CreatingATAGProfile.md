# Creating A TAG Profile #
In order to work with digital IDs and contracts you need to connect successfully to a Neuron server.
To do that an account, or **TAG Profile**, needs to be built. This is done step by step,
adding more information until it is complete. There are currently five (5) steps to take in the user registration journey:
1. Choose an operator (which domain to connect to)
2. Create an account
3. Register an identity (name, adress, et.c.)
4. Have the identity validated, either by an administrator or your peers
5. Choose a PIN to protect your account (optional).

The TAG Neuron SDK for Xamarin provides this, have a look at the [`ITagProfile`](../Tag.Neuron.Xamarin/Services/ITagProfile.cs) interface.
You can access it via the `DependencyService` class after registering it during startup. See the [Getting Started](GettingStarted.md#the-tag-neuron-sdk-structure) for details on how to do that.
The `ITagProfile` class has a `Step` property to indicate where in the registration process the user is.
It also has `set` and `clear` methods to set and clear each of the five steps.

## Registration UI ##
When the app is starting it detects whether this is a first-time run, or whether the **TAG Profile** has already been created 
(see the [`LoadingViewModel`](../IdApp/IdApp/ViewModels/LoadingViewModel.cs) for details).
If this is the first-time run, then the [`Registration Page`](../IdApp/IdApp/Views/Registration/RegistrationPage.xaml) is shown.
The registration page has a [CarouselView](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/carouselview/) that displays the
different registration steps in order. Think of it as a wizard. The user can go forward, but also back, until the registration is complete.

## Server Connection and Life Cycle Management ##
Assuming the **TAG Profile** has been completed, the app establishes a connection to the Neuron server and _keeps it alive_ for the duration of
the user session. When the app shuts down it closes the connection. This is different from most apps where individual HTTP requests are made, but between
calls 'nothing' really happens. Assuming you followed the steps in the [Getting Started](GettingStarted.md#creating-the-app) guide this will work out of the box.

