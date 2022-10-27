# OSCQuery Quickstart
## Installation into your project
To use OSCQuery in your project, you first need to integrate the library. This can be done by either installing it via NuGet *(not yet available)* or by building the .dll file yourself after pulling this repository.

**Disclaimer:**
OSCQuery can itself neither receive nor send OSC, it's purpose is to allow OSC services to find other services and to communicate to them what they can do. If you are looking to send OSC I recommend using either **[OscCore](https://github.com/vrchat/OscCore)**  for use within Unity or  **[Rug.Osc](https://bitbucket.org/rugcode/rug.osc/src/master/)** for anything else.

## Starting the OscQueryService
Before actually starting up the service, you first need two ports: 
- **UDP Port:** This port will be the port your service will advertise as OSC target port *(The port you will be listening on)*
- **TCP Port:** This port will host all the information about your service.

If you do not have any ports in mind you can easily find free ports by doing the following:
```csharp
var udpPort = Extentions.GetAvailableUdpPort();
var tcpPort = Extentions.GetAvailableTcpPort();
```

To then start the OSCQueryService you only need to add this:
```csharp
var queryService = new OSCQueryService("MyService", tcpPort, udpPort);
```
*Creating the Service also allows middleware or a logger as optional parameters if needed*

A full example for starting could look like this:
```csharp
var udpPort = Extentions.GetAvailableUdpPort();
var tcpPort = Extentions.GetAvailableTcpPort();
var queryService = new OSCQueryService("MyService", tcpPort, udpPort);

//Manually logging the ports to see them without a logger
Console.WriteLine($"Started QueryService at TCP {tcpPort}, UDP {udpPort}");
//Stops the program from ending until a key is pressed
//Only use this if the program would immedeately end without
Console.ReadKey();
```
*Note that example above is very simplified, you likely want the variables for ports and service to be accessible outside the function too*

The service should now be running. You can check this by opening a browser and entering `localhost:[tcpPort]` as URL.
You can get some additional info by instead using `localhost:[tcpPort]?HOST_INFO`

The service should automatically be advertising itself to other services. If you for some reason need to do it yourself you can use:
```csharp
queryService.AdvertiseOscService("MyService", udpPort);
```
*This can be used if you start up your Service without an udpPort originally and only want to advertise it once your listener is created, for example*

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

Well what if we also want to add services that might start up in the future? For that we have the `OnOscQueryServiceAdded` event. We can simply "subscribe" to it:
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
This, of course, only runs once. To make it repeat every 5 seconds we will be using the `Timer` class from `System.Timers`:
```csharp
var refreshTimer = new Timer(5000);
refreshTimer.Elapsed += (s,e) =>
{
	queryService.RefreshServices();
}
refreshTimer.Start();
```

### Checking if a service has an endpoint
Let's say we are looking specifically for services in our list that have an endpoint `/cool/endpoint`. To get all endpoints of an `OSCServiceProfile` we can do the following:
```csharp
var tree = await Extentions.GetOSCTree(profile.address, profile.port);
```
*Note the await keyword. You can only do this in async context*

To then check our endpoint we can do this:
```csharp
var node = tree.GetNodeWithPath("/cool/endpoint");
``` 

So finally we can just do this:
```csharp
foreach (var profile in _profiles)
{
	var tree = await Extentions.GetOSCTree(profile.address, profile.port);
	var node = tree.GetNodeWithPath("/cool/endpoint");

	//Skip to next if node does not exist
	if (node == null) continue;

	//Do something else here
}
```