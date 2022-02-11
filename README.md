# MiningGame 

Infinite modifiable terrain implemented with marching cubes. Includes an inventory system and has two types of ground (grass & stone).
<br/><br/>
![a house](https://github.com/roopekt/MiningGame/blob/main/ReadmeData/house.png)

## Installation 

 1. Clone the repository
	```shell
	git clone https://github.com/roopekt/MiningGame.git
	```
2. Install Visual Studio
	https://visualstudio.microsoft.com/downloads/
3. Install Unity
	- Install Unity Hub from https://unity3d.com/get-unity/download
	- Using the hub, install  an editor with the version 2020.2.2f1 (LTS). Remember to check visual studio integration when prompted about additional packages.
4. Locate and open the project trough Unity Hub

## Controls 

### General 
- Press esc to close the game

### Moving 
- Turn camera by dragging with mouse
- Move with WASD keys
- Jump by pressing space

### Modifying the terrain 
- Hold the left mouse button to mine
- Hold the right mouse button to place from your hand (highlighted slot in your hotbar)

### Inventory and items
- Pick up items by walking onto them
- Change your hand slot using the scroll wheel
- Open or close the inventory by pressing E
- Pick up a stack by left clicking while your cursor is empty
- Drop a stack by left clicking a slot
- Pick up half of a stack by right clicking while your cursor is empty
- Drop one item by right clicking

## How it works 

### Terrain
1. The world is split into cubic chunks.
2. Furthest chunks are constantly unloaded and regenerated closer to the player.
3. The mesh for the chunk is generated using the Marching Cubes algorithm from 3D perlin noise with multiple layers.
4. Ground type (grass or stone) is decided based on air exposure and slope of the terrain.
5. If the player decides to modify the terrain, a raycast is shot and the noise values are increased or decreased spherically. The mesh for the chunk will be regenerated.

### Inventory
- The is a Slot class that has all the logic for moving items between slots.
- Anything that can hold items uses th Slot class: inventory, cursor in the inventory and items on the ground.
- There is a SlotGUI class that all the GUI that is the same across the game. This listens to a event in the Slot class to update.
- There is a ItemType class that descripes all the properties of a item, and a ItemTypeHandler which is a singleton scriptable object containg all the item types.

## License 

This project is distributed under the MIT License. See `LICENSE.txt` for more information.

<br/><br/>
![inventory and a item on the ground](https://github.com/roopekt/MiningGame/blob/main/ReadmeData/inventory.png)