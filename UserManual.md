# User manual for Z21Dashboard

# Table of Contents
*   [Introduction](#introduction)
    *   [Why Z21Dashboard?](#why-z21dashboard)
    *   [A Bit About the Technology](#a-bit-about-the-technology)
*   [The Z21 Family of Central Stations](#the-z21-family-of-central-stations)
    *   [Use of Protocol](#use-of-protocol)
    *   [z21/z21Start and Locked or Not](#z21z21start-and-locked-or-not)
*   [First Time You Start](#first-time-you-start)
*   [The Dashboard](#the-dashboard)
    *   [Move Windows Around](#move-windows-around)
    *   [Customize Your Dashboard (Show/Hide Windows)](#customize-your-dashboard-showhide-windows)
    *   [The Most Important Windows Explained](#the-most-important-windows-explained)
* [Known issues](#known-issues)
    * [Z21Dashboard is run in a virtual Windows machine in Virtualbox, but some widgets are not displayed correctly](#z21dashboard-is-run-in-a-virtual-windows-machine-in-virtualbox-but-some-widgets-are-not-displayed-correctly)]
*   [FAQ](#faq)
    *   [What does 'Locked' mean?](#what-does-locked-mean)
    *   [Can names from my multiMAUS or Z21 app be transferred to Z21Dashboard?](#can-names-from-my-multimaus-or-z21-app-be-transferred-to-z21dashboard)
    *   [Z21Dashboard Cannot Connect to My Z21](#z21dashboard-cannot-connect-to-my-z21)
    *   [Will you implement widgets for LocoNet and CAN bus functionality?](#will-you-implement-widgets-for-loconet-and-can-bus-functionality)
    *   [I Found a Bug in Z21Dashboard, What Do I Do?](#i-found-a-bug-in-z21dashboard-what-do-i-do)
    *   [I Think Z21Dashboard is Cool, But I'm Not Good at Networking etc., What Do I Do?](#i-think-z21dashboard-is-cool-but-im-not-good-at-networking-etc-what-do-i-do)

# Introduction

Welcome to Z21Dashboard. This guide helps you get started with using the program to monitor and control your model
railway via your Roco/Fleischmann Z21 central station.

## Why Z21Dashboard?

Z21Dashboard started as a 'just for fun project'. I am an IT-developer, and I thought it would be fun to write a
program for my z21Start so I could see which locomotives and turnouts I was controlling on my layout.

Then came all the ideas, and Z21Dashboard was born.

## A Bit About the Technology

Z21Dashboard is Open Source software, and it can be found in my Github repository, where both source code and
binary files are available.

Z21Dashboard is developed in C# and .NET 10. It is developed as a .NET MAUI Blazor Hybrid Windows application.

# The Z21 Family of Central Stations

You can connect Z21Dashboard to the entire family of Z21 central stations from Roco:

*   z21
*   z21Start
*   Z21
*   Z21 XL

In this guide, the term 'Z21' is used for all of them unless there is something unique to a specific model.

## Use of Protocol

Normally, the Z21 central stations are connected for controlling 2-rail model railways and the protocol DCC. The
Z21 central stations are of the multiprotocol type, as they allow the use of both DCC and Märklin Motorola. mfx (M4)
is not supported by the Z21 central stations.

From new, the Z21 central station is set up to use both DCC and Märklin Motorola. You or others may have changed
this in the Z21 using Roco's application 'Maintenance Tool', where you can specify which protocols it should
support.

Although you can switch between protocols in certain Z21Dashboard settings (based on what version of the firmware
that is installed on your Z21), this cannot overwrite the setting in the Z21. I.e., if the Z21 is set to only DCC,
choosing Märklin Motorola in Z21Dashboard does not change the protocol for a locomotive or turnout.

## z21/z21Start and Locked or Not

Generally, the z21 (white box, where the text on the front is 'z21', notice the lower case 'z') is unlocked, though
z21 units were previously produced that were locked, just as the z21Start (white box, where the text on the
front is 'z21Start') is. Two different Roco items with unlock codes can be purchased, item no. 10814 (wifi router
and unlock code) and 10818 (unlock code).

If your z21/z21Start is locked, it will not be possible to control locomotives and turnouts in Z21Dashboard.
However, you can easily view their status as you control them with your multiMAUS.

# First Time You Start

When you open Z21Dashboard, the widget 'Connection', which allows you to connect to your Z21 central statison, is
shown. The very first time, the the standard IP address of the Z21s is displayed (192.168.0.111). If your Z21 have
another IP-address, you must correct the displayed one. C´lick on the button 'Connect' to connect to your Z21. When
you start Z21Dashboard the next time, the IP address field will be pre-filled wíth the one you entered.

1.  Find the Connection window: At the top of the screen, you will see a window titled 'Connection'.
2.  Enter IP address: A standard IP address (192.168.0.111) is already entered in the field. If your Z21 has a
different address on your network, you must correct it. The program remembers the address you enter for next time.
3.  Click on the green 'Connect' button.
4.  If Z21Dashboard achieves connection to your Z21, the status badge in the top right corner of the window will
change to 'Connected' and turn green. The rest of your dashboard windows will now be displayed.

While Z21Dashboard attempts to connect to your Z21 central station, it will say 'Connecting' in the small gray
field at the top right of the Connection widget. If a connection is not achieved, it will say 'Not connected' again
after a few seconds.

## First time you connect
When you establish connection to your Z21 central station for the first time, Z21Dashboard will show all available 
widgets. They are slightly offset from each other. You can start by moving them around, and then click on the gear
icon at the top right to see the overview with widgets. Here you can hide the widgets you do not wish to see.

# The Dashboard

The entire area under the 'Connection' window is your personal canvas. Here you can arrange the various information
and control windows (widgets) exactly as you wish.

## Move Windows Around

It is easy to move a window:
1.  Find the header: Point your mouse at the title bar of the window you want to move (e.g., on the text 'Power
Monitor'). Your mouse cursor will change to a 'grabbing hand'.
2.  Click and Drag: Click and hold the mouse button down, and drag the window to a new position on the screen.
3.  Release: Release the mouse button to place the window. The program automatically saves your window positions,
so your layout looks the same the next time you start.

## Customize Your Dashboard (Show/Hide Windows)

You don't need to have all windows shown at once. You can easily turn on and off the ones you need.
1.  Open Settings: Click on the large gear icon (⚙️) in the top right corner of the program window. A popup menu
named 'Settings' will open.
2.  On/Off: In the menu, you see a list of all available windows with an on/off button next to each. Click the
button to show or hide a window. The change happens immediately on your dashboard in the background.
3.  Close Settings: Click 'Close' or the cross to close the settings menu.

## The Most Important Windows Explained

### The Connection Window
This is your control panel for the connection. From here you can:
*   Establish and disconnect the connection to your Z21.
*   Turn track power on and off with 'Track Power On' and 'Track Power Off'.
*   Activate Emergency Stop to stop locomotives.

Be aware that Emergency Stop is implemented differently in decoders. In some, it means the locomotive stops
immediately, and in others, it means the speed is set to 0 while braking delay is still active. It is recommended
that you test your locomotives so you know if you can truly use Emergency Stop. Alternatively, you can always turn
off the track power. til stop all driving with immediate effect.

### Locomotive Control
This is your digital controller for a single locomotive.
1.  Select a Locomotive: Enter the address of the locomotive you want to view or control in the 'Address' field.
2.  View Live Status: As soon as you have entered an address, the window will show the locomotive's current speed,
direction of travel, and which functions (F0-F31) are active. This information updates live, even if you control the locomotive from another controller (e.g., a MultiMAUS).
3.  Control the Locomotive: If your z21/z21Start is 'unlocked' (see elsewhere), you can use the slider to change
speed, click on 'Forward'/'Backward' to change direction, and click on the F-buttons to turn functions on/off.

When you have selected a locomotive (address field is not 0), you can click on the gear in the top right. A popup
window will now appear. The options in this window depend on whether your z21/z21Start is locked.

Locked or not, you can:
*   Give the locomotive a name. This name is then shown in Z21Dashboard together with the address
*   Specify a number of operating hours before service is required on the locomotive
*   See how long it is until service must be performed
*   Reset the service time counters when you have performed service

Unlocked:
*   Select protocol and the number of speed steps

Locked:
*   Select protocol. If you want to change the number of speed steps, you must do so on your multiMAUS

Note, if you have selected the Märklin Motorola version 1 protocol, only the function button for light will be 
active. If you have selected Märklin Motorola version 2, then the function button for light as well as F1 - F4
will be active.

### Locomotive operation time
This window gives a fantastic overview of the locomotives that are in use. It shows a list of all active
locomotives and keeps track of how long each one has been in operation. It is also here that you can give your
locomotives personal names.

Next to each locomotive is a button with a gear. This opens the dialogue with settings for the respective
locomotive. The options in this window depend on whether your z21/z21Start is locked.

Locked or not, you can:
* Give the locomotive a name. This name is then shown in Z21Dashboard together with the address
* Specify a number of operating hours before service is required on the locomotive. In the overview, the
runtime will have a yellow background when there is less than an hour until service must be performed. The
background becomes red when service has not been performed
* See how long it is until service must be performed
* Reset the service time counters when you have performed service
* Delete a locomotive from the overview

Unlocked:
*   Select protocol and the number of speed steps.

Locked:
*   Select protocol. If you want to change the number of speed steps, you must do so on your multiMAUS.

Note that the runtime is saved on your PC when you close the program. So the next time the program starts and
connects to your Z21, the counting of runtime continues.

Runtime counts when the speed is different from 0.

### Turnouts

This window shows the turnouts where you have changed their position with your multiMAUS.
Next to each turnout, a button for settings is shown. When you click on it, a popup dialogue opens that lets you
choose which protocol should be used for the decoder in the turnout.

In the popup, you can choose between 'DCC' and 'MM' for Märklin Motorola. You can also delete the turnout from the
overview.

### Voltage and Current Overview

This window shows a graph with two lines; one for voltage and one for current, so you can follow how it develops
over time.

By clicking on the button with the gear at the top right of the widget, you can change the interval at which the
graph updates.

Regardless of which interval you choose, Z21Dashboard receives updates continuously from the Z21 central station.
The widget remembers the highest value received, and when the interval has 'expired', the highest value is written
to the graph.

# Known issues

## Z21Dashboard is run in a virtual Windows machine in Virtualbox, but some widgets are not displayed correctly
Typically , it is the two locomotive widgets and turnout protocol widget that are not displayed correctly.
There is shown a turning arrow along with the text 'Checking Z21 lock state'.

This may be due to the "Pointing device" on the "System" tab in Virtualbox being set to "USB Tablet". Change
it to "PS/2 Mouse" and try again.

# FAQ

## What does 'Locked' mean?

If you have a z21start (the white model), it may be 'locked' from the factory. This means that it does not allow
driving and switching via the network.
*   If LocoControl shows 'Locked': You can still use Z21 Dashboard as a monitoring tool. You can see live status
for your locomotives, but you cannot control them from the program (buttons will be disabled). You can, however,
change protocol for locomotives.
*   If LocoControl shows 'Unlocked': All functions in the program are available, and you can control your
locomotives directly.
To unlock your z21start, you need an unlock code from Roco (item number 10818 or 10814).

## Can names from my multiMAUS or Z21 app be transferred to Z21Dashboard?

Unfortunately, that is not possible.

## Z21Dashboard Cannot Connect to My Z21

The obvious one: check that the IP address is entered correctly. Open a command prompt in Windows, and ping your
Z21. If the ping does not get a reply, then there is a network problem you need to solve first.

The first time you start Z21Dashboard, Windows' firewall will ask if it is okay that Z21Dashboard communicates out
on the network. You must answer Yes to this. If you answered No, then you must go into the Windows firewall and
give permission for Z21Dashboard to communicate out of your PC.

## Will you implement widgets for LocoNet and CAN bus functionality?

The short answer is no. The long answer is that I own a z21Start, and therefore I do not have the need for such
widgets, and I cannot test the functionality.

## I Found a Bug in Z21Dashboard, What Do I Do?

Go to the repository and create an Issue.

### I Don't Have a GitHub Account

At present, the home of the project is my repository on GitHub, and that is where the dialogue about Z21Dashboard
needs to be.

## I Think Z21Dashboard is Cool, But I'm Not Good at Networking etc., What Do I Do?

You must seek help from a friend or possibly in your model train club to get the necessary help to set up the
network and possibly your router.
