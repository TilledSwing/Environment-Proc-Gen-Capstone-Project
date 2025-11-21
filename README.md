Build and Run Instructions:
To build and run our program, Unity is required. Clone the repo, open in Unity, and select “Build and Run” from the File dropdown. You are presented with a main menu screen and from there you can either start up game mode or enter editing mode.

Game Mode:
In game mode, you have to be connected to Steam. 

If you press “Create Lobby”, then you create a local Steam lobby and enter a world as a game player in a pre-game lobby. You are presented with a host panel. By default, a random terrain will generate below you, but you can use the “Random” or “Load” buttons to either generate another random terrain or load a saved terrain. Connected clients automatically also load this. There is also an in-game lobby screen and chat screen. You can press the escape key to free your mouse and click into the chat screen to talk to other players. In the host panel there is also the lobby ID which you can send to your Steam Friends. The “Start” button can be used to start the game, at which point you and all remote clients will drop into the game and the pre-game lobby will disappear.

If you press “Join Lobby”, then if you have a valid Steam lobby ID entered in, then you will join the host as a remote client. As a client, you must wait for the host to start the game. Anytime the host edits the terrain, it is also loaded for the remote clients. While you do have the option to quit, the host holds all control over the game. You can also chat and see the in-game lobby like the host.

Once the game starts, read the in game menu for all the different player abilities. Collect idols to earn money and survive as long as you can against the monsters that hunt you down.

Editor Mode:
In editor mode you are presented with a screen where you can modify terrain settings with sliders and buttons. Hovering over these will present further information on what the settings do. There are also options for changing textures. On the navigation bar at the top, you can save your terrain, load in a terrain from the database (see MAMP below), or enter explore mode. When you select explore mode, you are presented with options to start a server, client, enter an IP, or enter a name. If no IP is provided, then you host / connect as a client over the local network. Starting server establishes the connection, starting client joins the connection as a player. If an IP is provided of a remote server (like through ZeroTier), then players can join remotely. See in-game help menu for all player options. There is also a lobby and chat provided like with the game mode. If no name was presented then the player is “Anonymous”. 


Additional Libraries / Systems Needed:
MAMP:
MAMP facilitates the local host server. You will need to download MAMP and drag the folder “sqlconnect” into the MAMP htdocs folder. This will run our created php scripts when the game requests it.  
Steam for the game lobby.

Supported Systems:
Windows
MacOS

