# VRC OSCQuery

[OSCQuery](https://github.com/Vidvox/OSCQueryProposal) is a protocol that makes it easier for OSC apps to find and communicate with each other.

![VRC OSCQuery Routing](https://user-images.githubusercontent.com/737888/186757739-9ceb0334-f512-414b-8c5d-2aaec6d7d451.png)

We're developing this implementation of the protocol for OSC app creators integrate into their own projects as we integrate it into [VRChat](https://vrchat.com).
We're building it in C# targeting .NET 6 and Framework 4.6 so it will work in Unity as well as cross-platform .NET projects.

## 🔨 Functionality

This library implements almost all of the [Core Functionality](https://github.com/Vidvox/OSCQueryProposal#core-functionality) as described in the proposal:
* Advertises the service type "_oscjson._tcp" on the local network over Zeroconf.
* HTTP 1.1 Server
* Parses request url paths for OSC methods
* Provides JSON objects describing those OSC methods
* Discards fragment of url path
* Returns HTTP Status / Error codes

This library does not yet return limited attributes based on query strings, like only returning the VALUE of an object for a url that ends in "?VALUE", and does not yet return all possible error strings.

## ⚡️ Basic Use

1. Build vrc-oscquery-lib into vrc-oscquery-lib.dll and add it to your project (will make this a NuGet package once it's ready for wider use).
2. Construct a new OSCQuery service with `new OSCQueryServiceBuilder().WithDefaults().Build()`. T optionally passing in the name, TCP port to use for serving HTTP, UDP port that you're using for OSC, and an ILogger if you want logs.
3. You should now be able to visit `http://localhost:tcpPort` in a browser and see raw JSON describing an empty root node.
    - You can also visit `http://localhost:tcpPort?explorer` to see an OSCQuery Explorer UI for the OSCQuery service, which should be easier to navigate than the raw JSON.
4. You can also visit `http://localhost:tcpPort?HOST_INFO` to get information about the supported attributes of this OSCQuery Server.
5. Next, you can call `AddEndpoint` on your service to add information about an available OSC method. Note that this library does not send or receive OSC messages directly, it is up to you to choose and implement an OSC Library.
6. After you have added an endpoint, you can its information by querying the root node again, or query for your method specifically. If you added an endpoint for the OSC address "/foo/bar", you would query this method directly at `http://localhost:tcpPort/foo/bar`.
7. To remove the endpoint, call the `RemoveEndpoint()` method on your OSCQueryService instance, passing in the OSC address as a string ("/foo/bar");
8. When you are done with the service, call `Dispose` to clean it up.

For a more detailed walkthrough see [Getting Started](getting-started.md)

---

## 🐱‍🏍Testing with VRChat

This functionality will be released in an update soon™. These instructions

After launching the OSCQuery-enabled client, VRChat will start up an OSCQuery Service if you have OSC turned on. Note that we've changed the functionality of the OSC toggle a bit so you may need to turn it on every time you launch for now. VRChat will start a TCP service at [http://localhost:9001](http://localhost:9001) by default, or whatever port you have specified with your launch arguments. You can visit this url in a regular web browser to see the plain JSON which is returned for a request to the root namespace.

You can use the Unity App example which is included in this repo as source as well as in [the Releases](https://github.com/vrchat-community/vrc-oscquery-lib/releases) to test automatically connecting to VRChat and sending text to the Chatbox or receiving outgoing data over OSC. 

Try opening up two instances of the app to see VRChat send to both automatically.

## ✨ Examples

### Unity App

You can find a Unity Project in `Exampless/OSCQueryExplorer-Unity` - you can open this up in Unity 2019.4.31f1. 

Latest Release: [OSCQueryExplorer-Unity 0.0.5 for Windows](https://github.com/vrchat-community/vrc-oscquery-lib/releases/download/0.0.5/OSCQueryExplorer-0.0.5-beta.1.zip)

This app has six active scenes, and a "SceneChanger" to switch between them:
* [Chatbox-Sender](#chatbox-sender)
* [Chatbox-Receiver](#chatbox-receiver)
* [Tracker-Sender](#tracker-sender)
* [Tracker-Receiver](#tracker-receiver)
* [Head-&-Wrist-Receiver](#head--wrist-receiver)
* [Monitor](#monitor) 
* [Advertise-&-Find](#advertise--find)

#### Chatbox-Sender

This scene demonstrates how to find an OSC receiver compatible with your data and send it. It is similar to how VRChat implements its find-and-send logic. All of the code is in a single MonoBehaviour - [ChatboxSender.cs](Examples/OSCQueryExplorer-Unity/Packages/com.vrchat.oscquery/Samples/Chatbox/ChatboxSender.cs).

https://user-images.githubusercontent.com/737888/196586397-31c4d862-f119-4dce-97a2-375b212f27ca.mp4

#### Chatbox-Receiver

This scene implements receving ChatBox messages in the same way that VRChat does for easy testing.

### Tracker-Sender

This scene uses the same logic to find an OSC receiver compatible with Tracking data and send it.

https://user-images.githubusercontent.com/737888/198946626-033c8192-9b55-4b37-ac33-115e9dcd0ceb.mp4

You can test it using a second window or device, which is running the [Tracking-Receiver](#tracking-receiver) scene.

To use it with VRChat:
1. Start VRChat in VR mode.
2. Start OSCQueryExplorer-Unity and press "OSC-Trackers" on the menu page.
3. Wait until the app connects to your VRChat client. Leave it on the T-Pose animation.
4. In VRChat, open your QuickMenu and press "Calibrate FBT".
5. Wait for the avatar to reload, then press both triggers on your VR controllers.
6. Back in OSCQueryExplorer, press "Test-Motion" to send some virtual tracker data to VRChat.

Known Issues:
- In a mirror, the movements will look backwards 😛
- Hands are still tracked by controllers, so elbow movements may not do much
- The example video is at potato resolution, sorry

Read more: [OSC Trackers for VRChat](osc-trackers.md).

### Tracker-Receiver

This scene implements receiving Tracking messages in the same way that VRChat does for easy testing.

### Head & Wrist Receiver

This scene implements receiving VR system tracking data for the head and wrists that VRChat sends out to aid in things such as pose solving or drift correction in OSC Tracker apps. This scene also implements receiving the OSC bundle timestamp that is made available with the tracking data.

![image](https://github.com/vrchat-community/vrc-oscquery-lib/assets/38249782/6ad25453-abc5-4c7c-bcad-f52e48c28c0c)

### Monitor

This scene advertises itself as a receiver of OSC data, which VRChat will find and connect to. All of the code is in a single MonoBehaviour - [MonitorCanvas.cs](Examples/OSCQueryExplorer-Unity/Packages/com.vrchat.oscquery/Samples/Monitor/MonitorCanvas.cs)

https://user-images.githubusercontent.com/737888/196583859-6616b260-87c7-43a9-b6cc-26cfc110fbfe.mov

### Advertise & Find

This scene advertises randomly named OSC and OSCQuery and Services, and shows the names and ports of any other services it finds on the network. Handy for seeing what's available and testing your own applications. Makes a nice quick phone-to-phone demo as well.

### Console Apps
The solution includes three simple examples to demonstrate and test functionality. They are both .NET 6 Console apps and should work on Windows, Mac and Linux, but have only been tested on Windows 10 so far.

https://user-images.githubusercontent.com/737888/186757165-e47f766f-3bc2-46b2-8580-8fd99c6ce6b9.mp4

<details>
<summary>

#### LogAdvertisers

</summary>

This program simply listens for OSCQuery and OSC Service advertisements on the local network and prints them to the console. For OSCQuery services, it will also print the node tree of the service.

</details>

<details>
<summary>
  
#### DataSender

</summary>
  
This program will advertise itself as an OSCQuery and OSC Service and provide 10 randomly-named int parameters with random values to test the remote reading of OSC methods and values.

![image](https://user-images.githubusercontent.com/737888/186544804-97c4b454-5a28-4538-9626-7a55a305a882.png)

When it starts, it generates a random name, TCP and OSC ports. It is possible that these ports are already occupied or are even the same (though unlikely). You can change the name and ports before pressing "Ok".

![image](https://user-images.githubusercontent.com/737888/186544882-9808cf29-d75f-4908-b043-bebd7a6d959f.png)

After you press ok, it will display the OSC addresses and values of 10 integer parameters. You can press the name of any address to change its value to a new random integer.
  
</details>

<details>
<summary>
  
#### DataReceiver
  
</summary>

![image](https://user-images.githubusercontent.com/737888/186545650-bf3698e8-9518-4f6b-9a20-981e39657b7a.png)

This program will start with a list of available OSCQuery services found on your local network. If one is found, you can choose it from the list and press "Connect".

![image](https://user-images.githubusercontent.com/737888/186545685-6c36937d-d8d0-4efc-899b-a1c5f17df1d7.png)

Once connected, the program should display the target OSCQuery service's name and TCP port at the top of its window, and list the methods and their values below that.

It regularly polls for updates and should show value changes soon after they occur on the target Service.

</details>

---

## 📝 To-Dos:
* Support query strings for choosing attributes to return

---

## 🏞 Roadmap:
1. ~~Internal Discovery and Iteration~~
2. ~~Integrate into VRChat Client~~
3. ~~Closed Beta Release to OSC App Developers~~
4. General Release of Library and VRChat Client integration
