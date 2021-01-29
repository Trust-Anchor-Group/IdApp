# ID App

The **ID App** (or *TAG ID App*) is a *white-label* Xamarin app written in C# provided by [Trust Anchor Group](https://trustanchorgroup.com/) (**TAG** for short).
It can be used to create custom apps based on [IEEE P1451.99](https://gitlab.com/IEEE-SA/XMPPI/IoT). This includes:

* Federated & distributed Digital IDs. (Globally scalable, interoperable.)
* Smart Contracts
* Interfaces for communication with devices.
* Ownership, claims & Provisioning.
* End-to-end encrypted communication.
* Peer-to-peer communication.

It is built with .NET Standard 2.0, Xamarin.Forms 4.8 (Shell) and relies heavily on the [IoTGateway](https://github.com/PeterWaher/IoTGateway) framework.

## Structure

The app is made up of the actual app itself as well as two frameworks:
- Tag.Neuron.Xamarin
- Tag.Neuron.Xamarin.UI

The first is a wrapper library around the [IoTGateway](https://github.com/PeterWaher/IoTGateway) framework, making it mobile app friendly, but also
to provide convenience code and app lifecycle support for the Xamarin platform.

The second is the very few base classes needed for correct structuring and binding of UI components for the common setup/teardown scenarios that occur
when browsing/using an app. Typically that means tying the Appearing/Disappearing events of a page to business logic.


## Documentation ##

The architecture overview as well as technical guides for implementing and using the TAG Neuron SDK in a Xamarin app can be found here:

[Getting Started](Content/GettingStarted.md)

[Neuron SDK](Content/NeuronSDK.md)

[Neuron Registration](Content/NeuronRegistration.md)

[Neuron SDK UI](Content/NeuronSDKUI.md)


## License

You should carefully read the following terms and conditions before using this software. Your use of this software indicates your acceptance of this 
license agreement and warranty. If you do not agree with the terms of this license, or if the terms of this license contradict with your local laws, 
you must remove any files from the TAG Xamarin App from your storage devices and cease to use it. The terms of this license are subjects of changes 
in future versions of the TAG Xamarin App.

You may not use, copy, emulate, clone, rent, lease, sell, modify, decompile, disassemble, otherwise reverse engineer, or transfer the licensed program, 
or any subset of the licensed program, except as provided for in this agreement. Any such unauthorised use shall result in immediate and automatic 
termination of this license and may result in criminal and/or civil prosecution.

The source code and libraries provided in this repository (including references to external libraries) is provided open and without charge for the following uses:

* For **Personal evaluation**. Personal evaluation means evaluating the code, its libraries and underlying technologies, including learning about underlying technologies.
Redistribution of artefacts or source code requries attribution to the [original source code repository](https://github.com/Trust-Anchor-Group/XamarinApp), as well as a 
license agreement including provisions equivalent to this license agreement.

* For **Academic use**. This includes research projects, student projects or classroom projects. Redistribution of artefacts or source code requries attribution to the 
[original source code repository](https://github.com/Trust-Anchor-Group/XamarinApp), as well as a license agreement including provisions equivalent to this license agreement. 
Attribution and reference in published articles is encouraged. If access to other technologies based on IEEE P1451.99 is desired, please [contact Trust Anchor Group AB](#contact).

* For **Security analysis**. If you perform any security analysis on the code, to see what security aspects the code might have, all we request of you, is that you 
maintain the information in a confidential manner, inform us of any findings privately, with sufficient anticipation, before publishing your findings, in accordance 
with *ethical hacking* guidelines. By informing us at least forty-five (45) days before publication of the findings, you provide us with sufficient time to address 
any vulnerabilities you have found. Such contributions are much appreciated and will be acknowledged. (Note that informing us about vulnerabilities in public fora,
such as issues here on GitHub, counts as publishing, and not private.)

* For **Commercial use**. Use of the white-label TAG Xamarin App for commercial use is permitted. Replication and re-publication of source code is permitted with
attribution to the [original source code repository](https://github.com/Trust-Anchor-Group/XamarinApp), as well as a license agreement including provisions equivalent 
to this license agreement.

**Note**: All rights to the source code are reserved and exclusively owned by Trust Anchor Group AB. Any contributions made to the TAG Xamarin App repository 
become the intellectual property of Trust Anchor Group AB.

This software is provided by the copyright holder and contributors "as is" and any express or implied warranties, including, but not limited to, the implied 
warranties of merchantability and fitness for a particular purpose are disclaimed. In no event shall the copyright owner or contributors be liable for any 
direct, indirect, incidental, special, exemplary, or consequential damages (including, but not limited to, procurement of substitute goods or services; loss 
of use, data, or profits; or business interruption) however caused and on any theory of liability, whether in contract, strict liability, or tort (including 
negligence or otherwise) arising in any way out of the use of this software, even if advised of the possibility of such damage.

The TAG Xamarin App is © Trust Anchor Group AB 2019-2020. All rights reserved.

## Contact

You can choose to contact us via our [online feedback form](https://lab.tagroot.io/Feedback.md), via [company e-mail](mailto:info@trustanchorgroup.com), or the
[author directly](https://www.linkedin.com/in/peterwaher/).