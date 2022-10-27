# VRC OSCQuery

[OSCQuery](https://github.com/Vidvox/OSCQueryProposal) is a protocol that makes it easier for OSC apps to find and communicate with each other.

![VRC OSCQuery Routing](https://user-images.githubusercontent.com/737888/186757739-9ceb0334-f512-414b-8c5d-2aaec6d7d451.png)

We're developing this implementation of the protocol for OSC app creators integrate into their own projects as we integrate it into [VRChat](https://vrchat.com).
We're building it in C# targeting .NET 6 and Framework 4.6 so it will work in Unity as well as cross-platform .NET projects.

## 🦜 Closed Beta Discord

If you can see this message, then you are invited to join the conversation in the [VRChat General Testing Discord server](https://discord.gg/qqZKQabd). Note that you won't see the `#oscquery` channel until an admin sees you join and gives you access, which may take up to 1 business day.

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
2. Construct a new OSCQuery service with `new OSCQueryService()`, optionally passing in the name, TCP port to use for serving HTTP, UDP port that you're using for OSC, and an ILogger if you want logs.
3. You should now be able to visit `http://localhost:tcpPort` in a browser and see raw JSON describing an empty root node.
    - You can also visit `http://localhost:tcpPort?explorer` to see an OSCQuery Explorer UI for the OSCQuery service, which should be easier to navigate than the raw JSON.
4. You can also visit `http://localhost:tcpPort?HOST_INFO` to get information about the supported attributes of this OSCQuery Server.
5. Next, you can call `AddEndpoint` on your service to add information about an available OSC method. Note that this library does not send or receive OSC messages directly, it is up to you to choose and implement an OSC Library.
6. After you have added an endpoint, you can its information by querying the root node again, or query for your method specifically. If you added an endpoint for the OSC address "/foo/bar", you would query this method directly at `http://localhost:tcpPort/foo/bar`.
7. To remove the endpoint, call the `RemoveEndpoint()` method on your OSCQueryService instance, passing in the OSC address as a string ("/foo/bar");
8. When you are done with the service, call `Dispose` to clean it up.

For a more detailed walkthrough [see here](https://github.com/vrchat-community/vrc-oscquery-lib/blob/docs/readme/getting-started.md?plain=1)

---

## 🐱‍🏍Testing with VRChat

You can opt-in to a version of the VRChat Client using Steam Beta with the password `R34SzEpf4xp3g81`, which gives you access to the beta branch `oq-cb`. Please do not share the password, we don't want people using this feature before we've had a chance to work with the OSC app creators on iteration and integration.

After launching this special beta client, VRChat will start up an OSCQuery Service if you have OSC turned on. Note that we've changed the functionality of the OSC toggle a bit so you may need to turn it on every time you launch for now. VRChat will start a TCP service at [http://localhost:9001](http://localhost:9001) by default, or whatever port you have specified with your launch arguments. You can visit this url in a regular web browser to see the plain JSON which is returned for a request to the root namespace.

You can use the Unity App example which is included in this repo as source as well as in [the Releases](https://github.com/vrchat-community/vrc-oscquery-lib/releases) to test automatically connecting to VRChat and sending text to the Chatbox or receiving outgoing data over OSC. 

Try opening up two instances of the app to see VRChat send to both automatically.

## ✨ Examples

### Unity App

You can find a Unity Project in `Exampless/OSCQueryExplorer-Unity` - you can open this up in Unity 2019.4.31f1. 

This app has two scenes: [VRC-Chatbox](#vrc-chatbox) and [OSCQueryReceiver](#oscqueryreceiver). 

#### VRC-Chatbox

This scene demonstrates how to find an OSC receiver compatible with your data and send it. It is similar to how VRChat implements its find-and-send logic. All of the code is in a single MonoBehaviour - [ChatboxCanvas.cs](Examples/OSCQueryExplorer-Unity/Packages/com.vrchat.oscquery/Samples/Chatbox/ChatboxCanvas.cs).

https://user-images.githubusercontent.com/737888/196586397-31c4d862-f119-4dce-97a2-375b212f27ca.mp4

#### OSCQueryReceiver

This scene advertises itself as a receiver of OSC data, which VRChat will find and connect to. All of the code is in a single MonoBehaviour - [ReceiverCanvas.cs](Examples/OSCQueryExplorer-Unity/Packages/com.vrchat.oscquery/Samples/Receiver/ReceiverCanvas.cs)

https://user-images.githubusercontent.com/737888/196583859-6616b260-87c7-43a9-b6cc-26cfc110fbfe.mov

### Console Apps
The solution includes two simple examples to demonstrate and test functionality. They are both .NET 6 Console apps and should work on Windows, Mac and Linux, but have only been tested on Windows 10 so far.

https://user-images.githubusercontent.com/737888/186757165-e47f766f-3bc2-46b2-8580-8fd99c6ce6b9.mp4


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
3. Closed Beta Release to OSC App Developers
4. General Release of Library and VRChat Client integration
