# Building from the Command Line #
There are two build scripts in the folder `Build`, one for each platform.
- [BuildAndroid](../Build/BuildAndroid.bat)
- [BuildIPhone](../Build/BuildIPhone.bat)

There are two scripts since they need different arguments for creating a signed app.

## Android ##
When building an Android app you need to specify a path to a Keystore (for signing), as well as a password for that keystore.

```
> BuildAndroid.bat /path/to/keystore keystorepassword
```

**Example**
```
> BuildAndroid.bat "C:\Users\JohnDoe\Documents\IdApp\TagDemo Cert\TagDemo Cert.keystore" P4ssw0rd
```

## iPhone ##
When building an iPhone app you need to specify a Mac host, a Mac user and a Mac password.

```
> BuildIPhone.bat mac-host mac-user mac-password
```
**Example**
```
> BuildIPhone.bat MyMac.local johndoe topsecret
```

This of course also means that you have [paired your Windows](https://docs.microsoft.com/en-us/xamarin/ios/get-started/installation/windows/connecting-to-mac/) machine to a Mac, i.e. the Mac host is available in the network.


