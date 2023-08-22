# VRC OSCQuery

[OSCQuery](https://github.com/Vidvox/OSCQueryProposal) is a protocol that makes it easier for OSC apps to find and communicate with each other.

![VRC OSCQuery Routing](https://user-images.githubusercontent.com/737888/186757739-9ceb0334-f512-414b-8c5d-2aaec6d7d451.png)

This implementation of the protocol is made for OSC app creators integrate into their own projects as we have integrated it into [VRChat](https://vrchat.com).
We've built it in C# targeting .NET 6 and Framework 4.6 so it will work in Unity as well as cross-platform .NET projects.

## 🔨 Functionality

This library implements almost all of the [Core Functionality](https://github.com/Vidvox/OSCQueryProposal#core-functionality) as described in the proposal:
* Advertises the service type "_oscjson._tcp" on the local network over Zeroconf.
* HTTP 1.1 Server
* Parses request url paths for OSC methods
* Provides JSON objects describing those OSC methods
* Discards fragment of url path
* Returns HTTP Status / Error codes

See the [GitHub Project](https://github.com/vrchat-community/vrc-oscquery-lib) for more information.