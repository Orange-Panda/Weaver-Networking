# Weaver Networking Engine
Weaver was a basic networking engine created for Unity. 

# ⚠️ Disclaimer
This package was created for educational purposes and is *not* intended for production use. If you are looking for a Unity networking solution you are enocuraged to look at [Fish-Net](https://fish-networking.gitbook.io/docs/), [Mirror](https://mirror-networking.gitbook.io/docs/), or [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/).

## Download and Installation
Options for installation:
- Option A: Package Manager with Git (Recommended)
	1. Ensure you have Git installed and your Unity Version supports Git package manager imports (2019+)
	2. In Unity go to Window -> Package Manager
	3. Press the + icon in the top left
	4. Click "Add package from Git URL"
	5. Enter the following into the field and press enter: 
```
https://github.com/Orange-Panda/Weaver-Networking.git
```
- Option B: Package Manager from Disk
	1. Download the repository and note down where the repository is saved.
	2. In Unity go to Window -> Package Manager
	3. Press the + icon in the top left
	4. Click "Add package from disk"
	5. Select the file from Step 1
- Option C: Import Package Manually
	1. Download the repository
	2. In Unity's project window drag the folder into the "Packages" folder on the left hand side beside the "Assets" folder

## Quick Start Guide
1. Under the releases tab of this GitHub download the Examples.zip folder and import the contents into your assets folder.
2. Create a new empty game object and add the "Network Core" script to the game object
3. Add the Arena Example to your scene.
4. Add the Network UI Example prefab into your scene.
	1. If prompted to import TextMeshPro, do so. It is not necessary to import TextMeshPro's Examples/Extras.
5. Move the camera to a location where you can see the arena.
6. Build the client and run several instances of the game connected to each other.
7. These example objects should provide good reference for how to interface with the Network Engine on your own.
	1. The main class to understand is "NetworkComponent"

## Limitations
* Scene switching is not possible.
* Packet sizes are limited to a specific length. As a result having too many objects or object data sent at once will cause data to be lost. 
	* While this limit can be raised it increases the possibility of overflowing the network pipeline, losing clients with slow or unreliable internet.
* Packets are sent as strings leading to a lot of wasted space on packets.
