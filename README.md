# Platformer Generation Prototype
This repository contains a build of our current Platformer Level Generation technique, used to merge the procedural generation of level layouts with the more hand-crafted appeal of individual rooms/experiences. 

Currently, the generation creates a grid of interconnecting rooms that connect organically with their neighbours, and allows the placeholder player to maneuver around the map and use their rocket launcher on the fully destructible terrain. 

No enemies or further mechanics are included within this version of the project, as this is intended more as a demonstration of the basics of the level generation process in its current early state.

# Controls
A and D: Move player left and right

Space: Jump and Double Jump (can also slide along and jump up vertical walls)

Left click: Shoot weapon

Right click: Slow-motion (Only when airborne)

Escape: Generate a new level and reposition the player

# Source
I have included within this repository the source code used in the actual generation of the levels: from the exporting of room templates to create resources to draw from for generation, to the parsing and actual generation of the level layout and room selection.
