# Network Plug and Play Configuration System
&copy; Copyright 2017 Conscia Norway AS
## Introduction
This system implements a system for configuration deployment and management of Cisco (and other) network devices 
similar in nature to APIC-EM and Cisco Prime compliance. It is in its very early stages of development and is not 
intended to replace Cisco's own products but instead to facilitate moving and existing network to a point when 
Cisco's tools can be run. For networks which really only need configuration management and day-zero deployment,
this tool may be all that a corporation needs and it is designed to be lightweight enough to run on Raspberry Pi 
(our prefered platform) or as a container on a Cisco ISR4000 router with a compute module.

## License
The license is still being determined internally, however it is expected the codebase will be released under either
an MIT license or Apache license. At the time of this writing, we still have yet to determine the legal status and
license compatibility with licenses of code used from other projects. Let's for the time being assume the license 
is "buyer beware".

## Current state
As of this time, the code is in early development and is being targetted at a specific customer.

### Platform
.NET Core 2.0 was chosen for this project which in some cases makes perfect sense and in other cases can be illogical.
As .NET Core 2 is so new the paper it's printed on is still warm, developing for the platform can be quite complex.
This is already the second major attempt at getting the codebase running, as Visual Studio vomited over building projects
that included more than one library. The resolution of the problem was to create a new solution and try again.

### Templating
For the moment, the templating system used is NVelocity from project called 
[https://github.com/castleproject/MonoRail](Castle MonoRail). We have no idea what it is or what it is intended for. 
NVelocity however is a port of the Apache Velocity engine which is used as the native engine for templating in Cisco's
APIC-EM and Cisco's Prime Infrastructure. While NVelocity is a complete and true to form port of the engine, it has some
short-comings which will need to be resolved. At the moment, the biggest problem is lack of support for the Java String type
which has functions like Split which accepts a regular expression as input. This is heavily used by VTL (velocity template 
language) users for parsing of IP addresses for example. Therefore, once more functionality is present in the system, it
will be a top priority to add the missing functions.

### tftp.net
The project currently depends on [Valks tftp.net library](https://github.com/Valks/tftp.net) for the TFTP server back-end. 
This project seems to be a fairly well written TFTP server that doesn't require direct filesystem support and although it 
doesn't operate using the .NET async pattern, is quite versatile and scalable in my testing.

### Architecture
The current architecture is
