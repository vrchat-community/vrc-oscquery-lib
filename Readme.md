# VRC OSCQuery

[OSCQuery](https://github.com/Vidvox/OSCQueryProposal) is a protocol that makes it easier for OSC apps to find and communicate with each other.

We're developing this implementation of the protocol for OSC app creators integrate into their own projects as we integrate it into [VRChat](https://vrchat.com).
We're building it in C# targeting .NET Standard 2.0 so it will work in Unity as well as cross-platform .NET projects.

## Functionality

This library implements almost all of the [Core Functionality](https://github.com/Vidvox/OSCQueryProposal#core-functionality) as described in the proposal:
* Advertises the service type "_oscjson._tcp" on the local network over Zeroconf.
* HTTP 1.1 Server
* Parses request url paths for OSC methods
* Provides JSON objects describing those OSC methods
* Discards fragment of url path
* Returns HTTP Status / Error codes

This library does not yet return limited attributes based on query strings, like only returning the VALUE of an object for a url that ends in "?VALUE", and does not yet return all possible error strings.

## Basic Use

## Things to Discuss

### Tracking OSC Values

One very handy (optional) feature of OSCQuery is the ability to retrieve the _current value_ for a given OSC method. This lets clients do things like updating their UI elements without requesting the server to re-send each value.

The way this is currently implemented is through an optional `Func<string>` 'getter' method which can be passed as a parameter to the `AddEndpoint` function. This method is stored in the `OSCQueryNode` which is constructed to represent this Endpoint. 

When a client requests the value of a method, this `OSCQueryNode` Invoke the getter method if it exists, as well as the getter methods of each of its child nodes. Once all the methods resolve, the latest values are sent to the client. This can result in a bit of a delay, especially for nodes closer to root, and depends on the getter methods passed in during construction to still be valid.

There's other ways to handle this, for example a simple pub/sub system where endpoints are expected to publish value updates directly to the OSCQueryService class itself. In the case of VRChat, we already require any class that wants to send OSC messages to do it through our central `VRCOSCHandler`, so a pub/sub system on the OSCQueryService could be used to route things to/from the rest of VRChat. 

<details>

<summary>

## Examples

</summary>

The solution includes two simple examples to demonstrate and test functionality. They are both .NET 6 Console apps and should work on Windows, Mac and Linux, but have only been tested on Windows 10 so far.

### DataSender

This program will advertise itself as an OSCQuery and OSC Service and provide 10 randomly-named int parameters with random values to test the remote reading of OSC methods and values.

When it starts, it generates a random name, TCP and OSC ports. It is possible that these ports are already occupied or are even the same (though unlikely). You can change the name and ports before pressing "Ok".

After you press ok, it will display the OSC addresses and values of 10 integer parameters. You can press the name of any address to change its value to a new random integer.

### DataReceiver

This program will start with a list of available OSCQuery services found on your local network. If one is found, you can choose it from the list and press "Connect".

Once connected, the program should display the target OSCQuery service's name and TCP port at the top of its window, and list the methods and their values below that.

It regularly polls for updates and should show value changes soon after they occur on the target Service.

</details>
