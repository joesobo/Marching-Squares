# Marching Squares v1.0
[![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=for-the-badge&logo=unity)](https://unity3d.com)

This is a small Unity-engine based program that I have been developing. It's primarily functions as a terrain generator, that I hope to use for a number of projects in the future. This program using the Marching Squares algorithm.

### Brief Explanation ###
Using a 2D array of noise we *march* through the points information creating *squares* out of the adjecent pieces of information. Based on the configuation of the active 8 points of the square, we can draw triangles around each of the shapes, essentially outline the difference in state using triangles. You can take this algorithm further by introducing a 3D or scaling the effect of the states to create smoother transitions, but I decided to keep it how it is for asthetics and simplicity. 

If you're interested in this subject more of which can be learned from these links that I used.

https://catlikecoding.com/unity/tutorials/marching-squares/

https://www.youtube.com/watch?v=0ZONMNUKTfU

https://www.youtube.com/watch?v=yOgIncKp0BE

- - - - 

## Highlights ##
Just a few things that I'm proud of learning and would like to highlight from my time spent on this project.  

* Compute shaders for GPU usage and multi-threading
* Editor for quick block creation
* Comprehensive understanding of the Marching Squares algorithm
* Achieving multi-material Marching Square integration
* Achieving chunking/regions for infinite terrain
* Achieving full world serialization for saving/loading worlds
* Achieving full terrain editor with multiple sizes, types, and shapes
* Achieving a semi-optimized collider generator
* Achieving a working perlin noise terrain example
* Includes a map for viewing different noise layers (somewhat auto-updates)

- - - -

## Controls ## 

* WASD - Move  
* Space - Jump  
* Left Click - Edit Terrain  
* Right Click - Select Menu Options
* M - Toggle Map
* [ - Zoom In
* ] - Zoom Out 
* Escape - Menu 

- - - -

## How to Use ## 

Open the Scenes/World scene for quick entry non-saving

Open the Scenes/LevelSelection scene for full saving mode

In the Scene view:

&nbsp;&nbsp;&nbsp;&nbsp;`Terrain Generation/Terrain Noise` is the noise example
  
&nbsp;&nbsp;&nbsp;&nbsp;`Terrain Generation/Terrain Map` is the map controls with ability to switch the render type
  
&nbsp;&nbsp;&nbsp;&nbsp;`Window/BlockMap` will open up the block editor window for creating adding a new block
  
    1. Click the plus button to add an empty block to the list
    
    2. Add BlockType to enum in BlockManager
    
    3. Fill in information (make sure texture is in the `Resources/Blocks` folder
    
    4. Select the BlocksMaterial shader to assign the TextureArray to
    
    5. Save as JSON array and Save Texture2DArray
    
    6. You should now be able to see the new block in game and use it within the terrain generation!
