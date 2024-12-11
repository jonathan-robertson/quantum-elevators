# Quantum Elevators

[![🧪 Tested with 7DTD 1.2 (b27)](https://img.shields.io/badge/🧪%20Tested%20with-7DTD%201.2%20(b27)-blue.svg)](https://7daystodie.com/)
[![✅ Dedicated Servers Supported ServerSide](https://img.shields.io/badge/✅%20Dedicated%20Servers-Supported%20Serverside-blue.svg)](https://7daystodie.com/)
[![✅ Single Player and P2P Supported](https://img.shields.io/badge/✅%20Single%20Player%20and%20P2P-Supported-blue.svg)](https://7daystodie.com/)
[![📦 Automated Release](https://github.com/jonathan-robertson/quantum-elevators/actions/workflows/release.yml/badge.svg)](https://github.com/jonathan-robertson/quantum-elevators/actions/workflows/release.yml)

![quantum-elevators social image](https://raw.githubusercontent.com/jonathan-robertson/quantum-elevators/media/quantum-elevators-logo-social.jpg)

## Summary

7 Days to Die mod: Add infinite distance, vertical-warp elevator panels.

🔗 [Introductory video and demonstration of most features](https://youtu.be/fQPIQ9pdOrw)

### Support

🗪 If you would like support for this mod, please feel free to reach out via [Discord](https://discord.gg/hYa2sNHXya).

## Features

🛗 These vertical warp panels enable you to take full advantage of your land claim by allowing instant travel to any floor of your base - no matter how high in the sky or how deep in the ground you like to build.

If you place one of these panels **above or below another at any distance**, you can instantly travel between them by pressing the **Jump** or **Crouch** key to travel to the next panel in that direction!

There are two types of these panels available: Portable and Secure - you'll want to become familiar with each of them since they're best used in different situations (see sections below).

You can **craft either panel at the workbench** when you have the necessary items, or simply **purchase them from any trader** with cold, hard cash."

### Portable Quantum Elevator panels

🫳 Portable panels can be picked up after being placed, but **they must be fully repaired to do so**.

If you're trying to secure a base, it's highly recommended to use [Secure Quantum Elevator panels](secure-quentum-elevator-panels) instead. Even though other players won't be able to take your portable panels when placed within range of your LCB, **these panels have no locks or security features on them and would allow an attacking player to access other parts of your base you might want to keep secured**.

Instead, Portable Quantum Elevator panels are often the ideal choice when planning or constructing a base, building simple structures you might not need later, coordinating with other players to move quickly within a large city building, or setting an escape point partway through a quest to dump excess items off at your vehicle on the ground floor.

### Secure Quantum Elevator panels

🔐 These Panels **cannot be picked up once placed**, but they can be **locked**/**unlocked** and **set to a password**.

The password for each of the Secured Panels you want to travel to must either share the same owner and password as the panel you are traveling from, or you must be separately authenticated with that destination panel you're attempting to visit.

When attempting to travel, panels you are not already authenticated with or do not share the same owner and password as the panel you're traveling from will be skipped. In this case, the following panel in the sequence (if present) will be checked for authentication or you will be told you're already at the top/bottom floor.

> [Hint] You can use this mechanic to include **hidden rooms/floors** within your base's structure without it being overly clear that such a room exists. Simply set the panel in the 'hidden' room/floor to a different password from the rest.

### Admin Commands

> ℹ️ You can always search for this command or any command by running:
>
> - `help * <partial or complete command name>`
> - or get details about this (or any) command and its options by running `help <command>`

|     primary      | alternate |       params       | description                                                                                                                                            |
| :--------------: | :-------: | :----------------: | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| quantumelevators |    qe     |      `debug`       | enable/disable debug logging for this mod (disabled by default)                                                                                        |
| quantumelevators |    qe     | `push <x> <y> <z>` | recursively push entities 1 block at a time to make room; use float values from 'lp' command to let system calculate correct block coordinates for you |

*Note that leaving debug mode on can have a negative impact on performance. It is therefore recommended to only turn it on while troubleshooting and then disable it afterwards.*

## Setup

Without proper installation, this mod will not work as expected. Using this guide should help to complete the installation properly.

If you have trouble getting things working, you can reach out to me for support via [Support](#support).

### Environment / EAC / Hosting Requirements

| Environment          | Compatible | Does EAC Need to be Disabled? | Who needs to install? |
| -------------------- | ---------- | ----------------------------- | --------------------- |
| Dedicated Server     | Yes        | no                            | only server           |
| Peer-to-Peer Hosting | Yes        | only on the host              | only the host         |
| Single Player Game   | Yes        | Yes                           | self (of course)      |

> 🤔 If you aren't sure what some of this means, details steps are provided below to walk you through the setup process.

### Map Considerations for Installation or UnInstallation

- Does **adding** this mod require a fresh map?
  - No! You can drop this mod into an ongoing map without any trouble.
  - Because Quantum Elevators will show up in every trader's inventory, you may want to interact with each trader after installing this mod (and restarting the server/game) and opt to reset each trader's inventory. You can request this with Debug Mode enabled (run admin command `dm`, then close the admin console and hit `Q`; this enables the admin trader option to force an early restock).
- Does **removing** this mod require a fresh map?
  - Yes for the 2 key reasons:
    1. Some players will use elevators to travel into base locations that would otherwise be completely sealed off from the rest of the world. Suddenly removing/disabling the ability to do this would result in incredible frustration as some players would then need to break out of their bases and may not even have the tools necessary to do so. If this was the only reason for concern, removing Quantum Elevators mid-map would already be highly discouraged.
    2. Additionally, technical issues arise if players step on a panel's previous location after removing this mod (causing endless red errors in the client-side console, resulting in the player being locked in place). This is because Quantum Elevators uses the BuffWhenWalkedOn property in the 2 special blocks it adds, which is a common feature of many block-based 7 Days to Die mods. Removing any block of this type from the game after it has been placed will cause the same issue and

### Windows PC (Single Player or Hosting P2P)

> ℹ️ If you plan to host a multiplayer game, only the host PC will need to install this mod. Other players connecting to your session do not need to install anything for this mod to work 😉

1. 📦 Download the latest release by navigating to [this link](https://github.com/jonathan-robertson/quantum-elevators/releases/latest/) and clicking the link for `quantum-elevators.zip`
2. 📂 Unzip this file to a folder named `quantum-elevators` by right-clicking it and choosing the `Extract All...` option (you will find Windows suggests extracting to a new folder named `quantum-elevators` - this is the option you want to use)
3. 🕵️ Locate and create your mods folder (if missing): in another window, paste this address into to the address bar: `%APPDATA%\7DaysToDie`, then enter your `Mods` folder by double-clicking it. If no `Mods` folder is present, you will first need to create it, then enter your `Mods` folder after that
4. 🚚 Move this new `quantum-elevators` folder into your `Mods` folder by dragging & dropping or cutting/copying & pasting, whichever you prefer
5. ♻️ Stop the game if it's currently running, then start the game again without EAC by navigating to your install folder and running `7DaysToDie.exe`
    - running from Steam or other launchers usually starts 7 Days up with the `7DaysToDie_EAC.exe` program instead, but running 7 Days directly will skip EAC startup

#### Critical Reminders

- ⚠️ it is **NECESSARY** for the host to run with EAC disabled or the DLL file in this mod will not be able to run
- 😉 other players **DO NOT** need to disable EAC in order to connect to your game session, so you don't need to walk them through these steps
- 🔑 it is also **HIGHLY RECOMMENDED** to add a password to your game session
  - while disabling EAC is 100% necessary (for P2P or single player) to run this mod properly, it also allows other players to run any mods they want on their end (which could be used to gain access to admin commands and/or grief you or your other players)
  - please note that *dedicated servers* do not have this limitation and can have EAC fully enabled; we have setup guides for dedicated servers as well, listed in the next 2 sections: [Windows/Linux Installation (Server via FTP from Windows PC)](#windowslinux-installation-server-via-ftp-from-windows-pc) and [Linux Server Installation (Server via SSH)](#linux-server-installation-server-via-ssh)

### Windows/Linux Installation (Server via FTP from Windows PC)

1. 📦 Download the latest release by navigating to [this link](https://github.com/jonathan-robertson/quantum-elevators/releases/latest/) and clicking the link for `quantum-elevators.zip`
2. 📂 Unzip this file to a folder named `quantum-elevators` by right-clicking it and choosing the `Extract All...` option (you will find Windows suggests extracting to a new folder named `quantum-elevators` - this is the option you want to use)
3. 🕵️ Locate and create your mods folder (if missing):
    - Windows PC or Server: in another window, paste this address into to the address bar: `%APPDATA%\7DaysToDie`, then enter your `Mods` folder by double-clicking it. If no `Mods` folder is present, you will first need to create it, then enter your `Mods` folder after that
    - FTP: in another window, connect to your server via FTP and navigate to the game folder that should contain your `Mods` folder (if no `Mods` folder is present, you will need to create it in the appropriate location), then enter your `Mods` folder. If you are confused about where your mods folder should go, reach out to your host.
4. 🚚 Move this new `quantum-elevators` folder into your `Mods` folder by dragging & dropping or cutting/copying & pasting, whichever you prefer
5. ♻️ Restart your server to allow this mod to take effect and monitor your logs to ensure it starts successfully:
    - you can search the logs for the word `QuantumElevators`; the name of this mod will appear with that phrase and all log lines it produces will be presented with this prefix for quick reference

### Linux Server Installation (Server via SSH)

1. 🔍 [SSH](https://www.digitalocean.com/community/tutorials/how-to-use-ssh-to-connect-to-a-remote-server) into your server and navigate to the `Mods` folder on your server
    - if you installed 7 Days to Die with [LinuxGSM](https://linuxgsm.com/servers/sdtdserver/) (which I'd highly recommend), the default mods folder will be under `~/serverfiles/Mods` (which you may have to create)
2. 📦 Download the latest `quantum-elevators.zip` release from [this link](https://github.com/jonathan-robertson/quantum-elevators/releases/latest/) with whatever tool you prefer
    - example: `wget https://github.com/jonathan-robertson/quantum-elevators/releases/latest/download/quantum-elevators.zip`
3. 📂 Unzip this file to a folder by the same name: `unzip quantum-elevators.zip -d quantum-elevators`
    - you may need to install `unzip` if it isn't already installed: `sudo apt-get update && sudo apt-get install unzip`
    - once unzipped, you can remove the quantum-elevators download with `rm quantum-elevators.zip`
4. ♻️ Restart your server to allow this mod to take effect and monitor your logs to ensure it starts successfully:
    - you can search the logs for the word `QuantumElevators`; the name of this mod will appear with that phrase and all log lines it produces will be presented with this prefix for quick reference
    - rather than monitoring telnet, I'd recommend viewing the console logs directly because mod and DLL registration happens very early in the startup process and you may miss it if you connect via telnet after this happens
    - you can reference your server config file to identify your logs folder
    - if you installed 7 Days to Die with [LinuxGSM](https://linuxgsm.com/servers/sdtdserver/), your console log will be under `log/console/sdtdserver-console.log`
    - I'd highly recommend using `less` to open this file for a variety of reasons: it's safe to view active files with, easy to search, and can be automatically tailed/followed by pressing a keyboard shortcut so you can monitor logs in realtime
      - follow: `SHIFT+F` (use `CTRL+C` to exit follow mode)
      - exit: `q` to exit less when not in follow mode
      - search: `/QuantumElevators` [enter] to enter search mode for the lines that will be produced by this mod; while in search mode, use `n` to navigate to the next match or `SHIFT+n` to navigate to the previous match

### Troubleshooting / Common Issues

⚠️ Because QuantumElevators contains a DLL file, you may have trouble uploading to a 7 Days to Die dedicated host. Some hosts will silently *remove* DLL files from mods or prevent them from being overwritten (i.e. updated) when they are uploaded via FTP or various other methods. Please be sure to double-check that the `QuantumElevators.DLL` file is found within the `quantum-elevators` folder within your server if you don't see a reference to this DLL file in the logs on startup. In those cases, you can reach out to your host and explain the problem; most hosts will allow you to upload DLL mods and may have a special set of steps for you to follow or they may need to simply enable this functionality on your account.

## Special Thanks

Several people in the community have offered feedback, identified bugs, and have worked to provide me with debug logs to help move this project forward. Quantum Elevators is a cool idea and has been a lot of fun to work on, but would not be what it is today without the added effort from these incredible admins, modders, and players. *(Discord Usernames)*

- `Shavick#8511` performed early testing and identified various issues that arose when client and server were both running the mod.
- `Grandpa Minion#2643` and the NAPVP Community who helped with hardcore testing and several bug reports.
- `Blight#7410` of Pimp my House, Tea Lounge, Juggernaut, and Ragnarok submitted bug reports that helped identify and resolve a critical bug.
- `O C#2804` reviewed and offered suggestions and advice that led to the resolution of a critical bug.
- `vivo#0815` identified how to recreate a bug that occurred only on initial launch of a new map.
- `Oggy#9577` identified an issue related to height detection of multi-dimensional block rotation.
