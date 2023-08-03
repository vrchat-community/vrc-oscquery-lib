# OSCQuery Quickstart
## Installation into your project
To use OSCQuery in your project, you first need to integrate the library. This can be done by either installing it via NuGet *(not yet available)* or by building the .dll file yourself after pulling this repository.

**Disclaimer:**
OSCQuery can itself neither receive nor send OSC, its purpose is to allow OSC services to find other services and to communicate to them what they can do. If you are looking to send OSC you can use any OSC library, like **[OscCore](https://github.com/vrchat/OscCore)**  for Unity projects and **[Rug.Osc](https://bitbucket.org/rugcode/rug.osc/src/master/)** for .NET.

## Starting the OscQueryService

The OSCQueryServiceBuilder has a [Fluent Interface](https://en.wikipedia.org/wiki/Fluent_interface) for creating and configuring the OSCQuery service. To start a service that does "all the things" using the typical settings, you can call:

```csharp
var oscQuery = new OSCQueryServiceBuilder().Build();
```
This creates the Service, starts up the HTTP server on "localhost", starts up the Discovery system using the default library ([MeaMod.DNS](https://github.com/meamod/MeaMod.DNS)), and advertises both the OSCQuery and OSC services on the local network.

The format is always `new OSCQueryServiceBuilder()`, followed by all the things you want to add, finished with `Build()`, which returns the OSCQueryService you've just defined.

### Fluent Interface Options
There's a lot of options you _can_ configure if you want more control over what happens. The additional methods are listed below. Note that if you do not add any fluent options, then `WithDefaults()` is called for you automatically.

* WithDefaults()
  * Sets up Discovery, Advertising and HTTP serving using default names and ports.
* WithTcpPort(int port)
  * Set the TCP port you want to use for serving the HTTP endpoints. Defaults to any available open TCP port.
* WithUdpPort(int port)
  * Set the UDP port on which you're going to run an OSC Server. Defaults to any available open UDP port.
* WithHostIP(IPAddress address)
  * Set the address to use for serving HTTP. Defaults to localhost - note that serving to 0.0.0.0 on Windows is not allowed by default without jumping through some security hoops on each installed machine (this works on Android, though that implementation is not yet released).
* WithServiceName(string name) 
  * Sets the name that your service will use when advertising. Defaults to "OSCQueryService"
* WithLogger(ILogger<OSCQueryService> logger) 
  * Sets the target logger, which you can implement if you want to specifically log to the Console, or to a Unity textfield, or anything else for which you write an ILogger implementation.
* WithMiddleware(Func<HttpListenerContext, Action, Task> middleware) 
  * Adds Middleware to your HTTP server if you want to serve up additional pages or content.
* WithDiscovery(IDiscovery d) 
  * Sets the class to be used for advertising and discovering your service. Only for advanced users or other platforms - the Android-compatible implementation uses the native [NsdManager](https://developer.android.com/reference/android/net/nsd/NsdManager) class, for example (still in development).
* StartHttpSever()
    * Serves the HTTP endpoints required by the OSCQuery spec. You don't _have_ to call this if you're just using this library to find and receive data from other OSCQuery services, and not serving data yourself, but your app will be out-of-spec and may not play well with others.
* AdvertiseOSC() 
  * Broadcasts the info of the OSC Service on the local network.
* AdvertiseOSCQuery() 
  * Broadcasts the info of the OSCQuery Service on the local network.
* AddListenerForServiceType(Action\<OSCQueryServiceProfile\> listener, OSCQueryServiceProfile.ServiceType type)
  * Adds a listener which will be sent OSCQueryServiceProfiles for newly-discovered OSC or OSCQuery services.

You can can add these onto `.WithDefaults()` if you want _almost_ all the defaults. For example, this code will have all the defaults, but find the first available TCP port instead of 8060, and uses the name "MyService" instead of "OSCQueryService".

```csharp
var oscQuery = new OSCQueryServiceBuilder()
    .WithDefaults()
    .WithTcpPort(Extensions.GetAvailableTcpPort())
    .WithServiceName("MyService")
    .Build();
```
## A Simple Example

A minimal example for a working OSCQuery Service could look like this:
```csharp

var tcpPort = Extensions.GetAvailableTcpPort();
var udpPort = Extensions.GetAvailableUdpPort();

var oscQuery = new OSCQueryServiceBuilder()
    .WithDefaults()
    .WithTcpPort(tcpPort)
    .WithUdpPort(udpPort)
    .WithServiceName("MyService")
    .Build();

// Manually logging the ports to see them without a logger
Console.WriteLine($"Started OSCQueryService at TCP {tcpPort}, UDP {udpPort}");

// Stops the program from ending until a key is pressed
Console.ReadKey();
```
*Note that example above doesn't actually send or receive OSC, and will advertise that it has no endpoints available.*

The service should now be running. You can check this by opening a browser and entering `localhost:[tcpPort]` as URL.
You can get some additional info by instead using `localhost:[tcpPort]?HOST_INFO`

The service will advertise itself on the local network.

Finally, to manually stop the service you can use:
```csharp
queryService.Dispose();
```

## Adding endpoints to the service
Endpoints can be used to tell other OscQueryServices what your program can receive. To add one you can simply use this while the service is running:
```csharp
queryService.AddEndpoint("/my/fancy/path", "s", Attributes.AccessValues.WriteOnly, "This is my endpoint");
```
The parameters do the following:
- `"/my/fancy/path"` indicates the path of the OSC endpoint
- `s` indicates the type of variable *(string)* that is expected, a list can be found **[here](https://github.com/vrchat-community/vrc-oscquery-lib/blob/main/vrc-oscquery-lib/Attributes.cs)**
- `Attributes.AccessValues.WriteOnly` indicates that the endpoint can receive data, a list of those can also be found at the link above
- `"This is my endpoint"` is the endpoint's description

Alternatively, you can also specify a type of variable by calling it like this:
```csharp
queryService.AddEndpoint<string>("/my/fancy/path", Attributes.AccessValues.WriteOnly, "This is my endpoint");
```

After adding an endpoint you can either find it at `localhost:[tcpPort]`, `localhost:[tcpPort]/path` or, `localhost:[tcpPort]?explorer`

To remove the endpoint again, simply call:
```csharp
queryService.RemoveEndpoint("/my/fancy/path");
```

## Interacting with other services
### Finding other services and saving them in a List
Let's say your program needs a list of all other services that are currently running.
```csharp
private List<OSCQueryServiceProfile> _profiles = new();
```
This is the list we will be using to store information about all other services. The class `OSCQueryServiceProfile` is the class containing all information about another Service.

We have also create the following function to store another profile in our list. We also log its name in the console to let us know it was added.
```csharp
private void AddProfileToList(OSCQueryServiceProfile profile)
{
	_profiles.Add(profile);
	Console.WriteLine($"Added {profile.name} to list of profiles");
}
```

To then get all currently running services we can just use `GetOSCQueryServices()`. We will be using it to add each one to our list:
```csharp
foreach(var service in queryService.GetOSCQueryServices())
{
	AddProfileToList(profile);
}
```

To add services that start up in the future, you can subscribe to the `OnOscQueryServiceAdded` event:
```csharp
queryService.OnOscQueryServiceAdded += (var profile) => 
{
	AddProfileToList(profile);
}
```
This will automatically add new services to the list when found after a refresh.
```csharp
queryService.RefreshServices();
```
You can use the `Timer` class from `System.Timers` to repeat this refresh every 5 seconds:
```csharp
var refreshTimer = new Timer(5000);
refreshTimer.Elapsed += (s,e) =>
{
	queryService.RefreshServices();
}
refreshTimer.Start();
```

### Checking if a service has an endpoint
You can get use the `GetOSCTree` function to retrieve the full OSC Tree of any service in order to look through it:
```csharp
var tree = await Extensions.GetOSCTree(profile.address, profile.port);
```
*Note the await keyword. You can only do this in an [async context](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/), as this actually makes a request across the network.*

You can then use `GetNodeWithPath()` to check whether the OSC Tree has a particular endpoint, like `/cool/endpoint`. This will return null if nothing is found, or the name, description, access and contents, etc of the node if it is found.
```csharp
var node = tree.GetNodeWithPath("/cool/endpoint");
``` 

You can combine the methods above to check a list of discovered profiles for a particular endpoint like this:
```csharp
foreach (var profile in _profiles)
{
	var tree = await Extensions.GetOSCTree(profile.address, profile.port);
	var node = tree.GetNodeWithPath("/cool/endpoint");

	// Skip to next if node does not exist
	if (node == null) continue;

	// Do something else here, like subscribing to the endpoint
}
```
