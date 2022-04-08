
# Custom Games Mod 2

A mod for the Attack On Titan Tribute Game.  
Created by Avisite.

[Download Latest Version](https://github.com/KaneMcGrath/CustomGamesMod/releases/tag/v1.0)

# Overview
The Custom Games Mod is a mod for the game Attack on Titan Tribute Game that focuses on "Server Side" Improvements to the game.  
This means using the games functions in ways that were not intended, in order to create new experiences for anybody playing the game.  
This mod currently aims at compatibility  with the RC Mod and mods that are based on it.  But because some of the ways this mod 
works are really some weird hacks, some mods may handle this mod differently, or not work at all.

This mod is built on an old version of the RC mod, and is missing some newer features and improvements.  
But it should still be compatible with the 11/22/2021 update.  
Ill try to update this mod to the latest version of the RC mod later on.
This mod is still in development, and has many bugs.

The Custom Games Mod currently has two major features

### Custom Level Manager
A new level manager that allows loading custom levels on any map
add objects and structures to The City, The Forest, or even Outside the Walls.
Maps are saved as files, and you can select from a list of saved maps when starting a game.

### Soccer

A custom gamemode that implements a working soccer game.  
Inspired by Rocket League, two teams compete to score points by hitting the ball into the enemy teams goal.
Includes options to disable hooks and to change the physics properties of the ball.
As well as a team manager UI to help manually assign and balance teams.

# Custom Level Manager
With the CG Custom Level Manager you can load custom levels on any map and add unique structures or features to them.  
you can even disable the map bounds and extend some maps beyond their borders (Doesn't really work on Outside The Walls)  
spawners and respawn points dont work.

In the Custom Map tab click on "CGCustom Level Manager".  
Check the "Enable CG Level Manager" checkbox, then select a map below.  You can preview the map before loading it.
Hit "Restart and Apply" to load the map.

Some small improvements to the level editor have been made, most importantly the ability to load any map you want.
As well as being able to save maps to a file.  Maps are stored in the "levels" folder.
# Soccer

***Soccer is counterintuitively not compatible with the CG Level Manager, make sure to load the map the normal way***

This custom gamemode adds a fully functioning soccer game.  
Inspired by Rocket League, two teams compete to score points by hitting the ball into the enemy teams goal.

the ball is a cannonball that will bounce off of players that hit it.  
Because it is small and hard to see, there is a bomb that floats above it at a fixed height.  
It will also appear as a titan on the minimap.

because all of the physics are being calculated by the host, there is a significant delay to other players.  
to help compensate for this, all players have a large hitbox.  The hitbox is like a rounded cube that points towards
your velocity vector.  So even if you are a bit off, the ball will still go straight when you hit it.

## Game Settings
```
[ Start Gamemode ]
 this button enables the mod functions and starts the game.
[ Reset Score ]
 This will reset the score to 0 for both teams and start a new game.
[ Restart Game ]
 Will start a new game without affecting the score.

______Ball Properties______

Radius:				        Size of the hitbox around players.
Max Speed:				The maximum speed the ball can travel.
Force Multiplier:			the amount of force the ball will have when it hits a player.  Multiplied by the players velocity
Friction:				Changes the friction component of the balls PhysicMaterial.  Will make long shots harder.
Bounciness:				Changes the bounciness component of the balls PhysicMaterial.  Will make the ball bounce higher.

______Game Properties______

[ Reset Ball ]				Moves the ball back to the center.  Useful if the ball gets stuck.
[ Manage Teams ]			Brings up the team manager window.  Used to manually assign teams.
[x] Send Welcome Message		Sends a message to players when they join the game.  You can edit the message in CGConfig\JoinMessage.txt
Goal Explosion Force:			How much force to apply to the player when the ball hits the goal.
Titan Height:				Controls the height of the titan used for the minimap.  Keep him below the map.
Bomb Height:				Controls the height of the Bomb used to indicate the balls position.
[x] Disallow Hooks			Will kill players when they try to fire hooks.  Slows down the pace of the game.

[ Apply ]				Hit this button to apply any changes.
```
All settings for this mod are stored in CGConfig\Settings.txt and are saved and loaded with the Save and Load Buttons in the pause menu
## Custom Maps
The provided Map SoccerField.txt is pretty good, but you are welcome to make your own map.  
To be able to work with this mod you need to include regions with the same name
 - "GoalBlue"			Goal on the cyan side of the map.
 - "GoalRed"			Goal on the Magenta side of the map.
 - "BallSpawn"			Where the ball spawns and is reset to.
 - "CyanGas"			When a black flare is fired while a player is still, they will be teleported here.
 - "MagentaGas"		When a black flare is fired while a player is still, they will be teleported here.


# Source Code

This mod is open source and you are welcome to include or improve any parts of it in your own mods.
As long as credit is given.

