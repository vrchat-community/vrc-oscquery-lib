# VRC OSCQuery

[OSCQuery](https://github.com/Vidvox/OSCQueryProposal) is a protocol that makes it easier for OSC apps to find and communicate with each other.

We're developing this implementation of the protocol for OSC app creators integrate into their own projects as we integrate it into [VRChat](https://vrchat.com).
We're building it in C# targeting .NET Standard 2.0 so it will work in Unity as well as cross-platform .NET projects.

## Status

![NUnit Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/vrchat-developer/4c1497eb43ee225c377c964b2e447a89/raw/test.json)

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

1. Build vrc-oscquery-lib into vrc-oscquery-lib.dll and add it to your project (will make this a NuGet package once it's ready for wider use).
2. Construct a new OSCQuery service with `new OSCQueryService()`, optionally passing in the name, TCP port to use for serving HTTP, UDP port that you're using for OSC, and an ILogger if you want logs.
3. You should now be able to visit `http://localhost:tcpPort` in a browser and see raw JSON describing an empty root node.
4. You can also visit `http://localhost:tcpPort?HOST_INFO` to get information about the supported attributes of this OSCQuery Server.
5. Next, you can call `AddEndpoint` on your service to add information about an available OSC method. Note that this library does not send or receive OSC messages directly, it is up to you to choose and implement an OSC Library.
6. After you have added an endpoint, you can its information by querying the root node again, or query for your method specifically. If you added an endpoint for the OSC address "/foo/bar", you would query this method directly at `http://localhost:tcpPort/foo/bar`.
7. To remove the endpoint, call the `RemoveEndpoint()` method on your OSCQueryService instance, passing in the OSC address as a string ("/foo/bar");
8. When you are done with the service, call `Dispose` to clean it up

---

## Things to Discuss

### Tracking OSC Values

One very handy (optional) feature of OSCQuery is the ability to retrieve the _current value_ for a given OSC method. This lets clients do things like updating their UI elements without requesting the server to re-send each value.

The way this is currently implemented is through an optional `Func<string>` 'getter' method which can be passed as a parameter to the `AddEndpoint` function. This method is stored in the `OSCQueryNode` which is constructed to represent this Endpoint. 

When a client requests the value of a method, this `OSCQueryNode` Invoke the getter method if it exists, as well as the getter methods of each of its child nodes. Once all the methods resolve, the latest values are sent to the client. This can result in a bit of a delay, especially for nodes closer to root, and depends on the getter methods passed in during construction to still be valid.

There's other ways to handle this, for example a simple pub/sub system where endpoints are expected to publish value updates directly to the OSCQueryService class itself. In the case of VRChat, we already require any class that wants to send OSC messages to do it through our central `VRCOSCHandler`, so a pub/sub system on the OSCQueryService could be used to route things to/from the rest of VRChat. 

---

## Examples

The solution includes two simple examples to demonstrate and test functionality. They are both .NET 6 Console apps and should work on Windows, Mac and Linux, but have only been tested on Windows 10 so far.

### DataSender

This program will advertise itself as an OSCQuery and OSC Service and provide 10 randomly-named int parameters with random values to test the remote reading of OSC methods and values.

![image](https://user-images.githubusercontent.com/737888/186544804-97c4b454-5a28-4538-9626-7a55a305a882.png)

When it starts, it generates a random name, TCP and OSC ports. It is possible that these ports are already occupied or are even the same (though unlikely). You can change the name and ports before pressing "Ok".

![image](https://user-images.githubusercontent.com/737888/186544882-9808cf29-d75f-4908-b043-bebd7a6d959f.png)

After you press ok, it will display the OSC addresses and values of 10 integer parameters. You can press the name of any address to change its value to a new random integer.

### DataReceiver

![image](https://user-images.githubusercontent.com/737888/186545650-bf3698e8-9518-4f6b-9a20-981e39657b7a.png)

This program will start with a list of available OSCQuery services found on your local network. If one is found, you can choose it from the list and press "Connect".

![image](https://user-images.githubusercontent.com/737888/186545685-6c36937d-d8d0-4efc-899b-a1c5f17df1d7.png)

Once connected, the program should display the target OSCQuery service's name and TCP port at the top of its window, and list the methods and their values below that.

It regularly polls for updates and should show value changes soon after they occur on the target Service.

---

## To-Dos:
* Figure out why HOST_INFO stopped working
* Make zeroconf advertising optional
* Support query strings for choosing attributes to return


---

## Roadmap:
1. Internal Discovery and Iteration
2. Integrate into VRChat Client
3. Closed Beta Release to OSC App Developers
4. General Release of Library and VRChat Client integration
