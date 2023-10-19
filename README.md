# moustachevania
Public repository for the source code from Moustachevania, available free for educational purposes.

# What is this?
Moustachevania is an game by Aaron Nemoyten, released publicly in a (probably permanently) unfinished state after years of on-and-off work. This repository is the original source code from the game - C# code and a few other bits and pieces, but (thus far) no Unity project, prefabs, assets, or dependencies. 

The WebGL build of the game can be found here:
https://swivelmaster.itch.io/moustachevania

# What can I do with this?
This code is provided for free for educational and noncommercial purposes. You are free to use and modify it as you wish for noncommerical and educational purposes. If you publicly release any projects that use this code, you must credit the original author (Aaron Nemoyten) and provide a link back to this repository (https://github.com/swivelmaster/moustachevania) in in-project accessible credits OR a bundled Readme file OR wherever the file is hosted (like the Description text on an itch.io page). 

(If for some reason the repository becomes unavailable, please make a good-faith effort to find a reasonable replacement link that would make a reasonable way to provide credit.)

If you wish you use any of this code for commercial purposes, please contact Aaron Nemoyten directly at aaron [at] nemo10.net

# What is included?
Quite a lot, actually.

Generalized utilities:
* Customized Unity tilemap tile implementations that handle many, MANY iterations of surrounding tile combinations
* SnapToGrid editor component
* A complete gameplay framework that handles state transitions, can push/pop state, and automatically suspends states that aren't at the top of the stack
* A set of scripts that handle parallax backgrounds with an orthographic camera by moving objects relative to camera position + their original world position (used only in the "Fancy" version of the game)
* A PixelPerfectCamera Aspect Ratio Fitter (Unity's latest pixel perfect camera may have rendered this obsolete, I haven't checked yet)
* A very useful DebugOutput class for printing debug info to the screen in a nicely-organized way

Stuff that can be generalized out to work with many games:
* A controller input manager that interacts with ReWired and passes control input to the currently active state
* Checkpoints and persistence/savegame code (Also see Destroyable and UniqueId)
* A Zone system that can be used to bulk switch objects on and off when the player enters and exits
* Object movement code that handles perfectly synchronized moving platforms and objects, triggers, one-time-use platforms that save their state, and triggered platforms that generate new copies of themselves when triggered
* Audio and music code that wraps DarkTonic's MasterSound asset
* Camera code/wrappers that work with ProCamera2D and Unity's post processing stack
* A dialogue system that integrates with Yarn 1.X and has a ton of custom functions

Extremely game-specific stuff:

* The AdjustableObject system used for the sets of tiles in the "Fancy" version of the game that can be manipulated (rotate, etc.) together.
* Complete player controller with multiple jumps, dash, teleport, input buffering, and a whole lot more
* A "reset sphere" object type that pauses the player's movement while resetting their jump/dash count mid-air
* Collection and inventory management (kind of hacky, sorry)
* Lots and lots of UI implementation examples

# Caveat For Licensing
There are a few pieces of code in the repository that are from public sites like Unity Answers, StackOverflow, etc. These are commented specifically. Their site's respective licensing terms apply.

Any assets provided as examples for code usage (example: Dialogue scripts) may not be re-released publicly and are merely provided for educational purposes.