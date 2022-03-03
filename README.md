# Guess That City Discord Bot

## To setup
1. Move into the `deploy` folder
2. Execute the `instal.sh` script to install docker, etc.
3. Configure `appsettings.json` in the root folder with the following keys:
   1. `DiscordToken` - The token for the discord bot
   2. `ArcgisToken` - An ArcGIS token to do geocoding
4. In the deploy folder execute `build.sh`