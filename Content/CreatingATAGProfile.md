# Creating A TAG Profile #

In order to work with digital IDs and contracts you need to connect successfully to a Neuron server.
To do that an account, or **TAG Profile**, needs to be built. This is done step by step,
adding more information until it is complete. There are currently five (5) steps to take in the user registration journey:
1. [Verify phone number](#verify-phone-number) (which suggests which domain to connect to)
2. [Create an account](#create-an-account)
3. [Register an identity](#register-an-identity) (name, adress, et.c.)
4. [Validate the identity](#validate-an-identity), either by an administrator or your peers
5. [Choose a PIN](#choose-a-pin) to protect your account (optional).

## 1. Verify phone number ##

In this step you need to provide the purpose for using the app, and provide a phone number. An SMS will be sent to the phone number
with a verification code that you need to enter, in order to continue. The app is also provided information about what domain it can
create accounts on, based on the country code given in the phone number.

## 2. Create an account ##

This is where you can do one of two things:

1. Create a new account, _or_
2. Scan an invitation code. Such a code may direct the app to another broker and predefined account.

## 3. Register an identity ##

This step only happens if you chose to create a _new_ account.

## 4. Validate an identity ##

When creating an account your identity must be validated. This can happen in two ways:
1. An operator of the Neuron server you're connecting to can validate it for you.
2. A peer can validate it, this is done by you requesting a peer review. This is done inside the app by either scanning a QR Code that belongs to your peer, or by manually entering the id. Once your peer or an operator has approved your identity, you can continue. Currently two peers are required to validate your identity.

## 5. Choose a PIN ##

Choosing a PIN is an optional step that can be skipped. Creating a PIN adds an extra layer of security.

The TAG Neuron SDK for Xamarin provides this, have a look at the [`ITagProfile`](../IdApp/Services/ITagProfile.cs) interface.
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
