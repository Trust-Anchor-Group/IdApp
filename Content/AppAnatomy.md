# Anatomy of the ID App #
The ID App showcases the creation of a Digital ID as well as contract handling.

Here's the overall app and page structure:

```

                         ----------
                        |          |
                        |  Reg.    |
 ----------             |  Page    |                ----------
|          |            |          |               |          |
| Loading- |   ------>  |          |               |  Other   |--
| Page     |_ /          ----------                |  Pages   |  |
|          |  \          ----------                |          |  |
|          |   ------>  |          |               |          |  |
 ----------             |  Main    |                ----------   |
                        |  Page    |                  |          |
                        |          |                   ----------
                        |          |
                         ----------

```
Upon every startup the [`LoadingPage`](../IdApp/IdApp/Views/LoadingPage.xaml.cs) is displayed. During this time the `TAG ID SDK` is initalized.
The [`ITagProfile`](../IdApp/Services/ITagProfile.cs) is checked for completeness. Does the user have a valid Digital ID?
If so, go to the [`MainPage`](../IdApp/IdApp/Views/MainPage.xaml.cs) once a connection to the Neuron server has been established. If not, then show the
[`RegistrationPage`](../IdApp/IdApp/Views/Registration/RegistrationPage.xaml.cs) instead. It will take the user through the neccessary steps to build and
validate its [`ITagProfile`](../IdApp/Services/ITagProfile.cs).

## Other Pages ##
There are various pages in the ID App to display client and server signatures, other users' profiles, digital contracts et.c.

Many of the pages are accessed via the [Flyout](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/shell/flyout) menu on the MainPage
 (see the [`AppShell.xaml`](../IdApp/IdApp/AppShell.xaml) for the visual hierarchy).

Others are handled via the [`IContractOrchestratorService`](../IdApp/IdApp/Services/IContractOrchestratorService.cs), which takes on the bulk of
the digital contracts. An example would be when receiving a peer review request, or when a contract has been revoked.