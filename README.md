#WorldDomination.RavenDb 
[![Build status](https://ci.appveyor.com/api/projects/status/43prv31nudiqixvn?svg=true)](https://ci.appveyor.com/project/PureKrome/worlddomination-ravendb)    
Client: [![](http://img.shields.io/nuget/v/WorldDomination.Raven.Client.svg?style=flat-square)](http://www.nuget.org/packages/WorldDomination.Raven.Client/) ![](http://img.shields.io/nuget/dt/WorldDomination.Raven.Client.svg?style=flat-square)    
Test Helpers: [![](http://img.shields.io/nuget/v/WorldDomination.Raven.Tests.Helpers.svg?style=flat-square)](http://www.nuget.org/packages/WorldDomination.Raven.Tests.Helpers/)![](http://img.shields.io/nuget/dt/WorldDomination.Raven.Tests.Helpers.svg?style=flat-square)


---

This library comprises of two packages to help your every day development of applications that use RavenDb.

The library offers two packages:
- `Client` : some basic DocumentStore extensions and listeners.
- `Tests.Helpers`: a class to reduce the ceremony required to create a test that connects to RavenDb.

---
## Installation

TODO:  image for nuget pics, etc.    
 !! split into two parts, one per nuget package

---
#Why use this package?
There's two scenario's why these NuGet packages exist.
1 - Client: A simple way to make setup your RavenDb document store with fake data. (eg. Only store fake data when the data doesn't exist, etc).
2 - Tests.Helpers: Reduce the ceremony (read: code) to create your (usually) InMemory document store specifically setup for _unit tests_. This includes a quick way to setup fake data, indexes (the test only require), etc.

---
[![I'm happy to accept tips](http://img.shields.io/gittip/purekrome.svg?style=flat-square)](https://gratipay.com/PureKrome/)  
![Lic: MIT](http://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)