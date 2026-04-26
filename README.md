# Space Engineers 2 Head Tracking Plugin
This plugin enables head tracking in Space Engineers 2 using the FreeTrack protocol.  Currently limited to cockpits/seats

## Prerequisites
- [OpenTrack](https://github.com/opentrack/opentrack) or another app that uses the FreeTrack protocol
- [Space Engineers 2](https://store.steampowered.com/app/1133870/Space_Engineers_2/)
- [Pulsar](https://github.com/SpaceGT/Pulsar)

## Configuration
Configuration is handled in the Plugins section of the SE2 application:
<img width="1585" height="1122" alt="image" src="https://github.com/user-attachments/assets/72863763-e9cd-47ff-9b04-1db6514f0a39" />

#### Multiplier
Use this to adjust the sensitivity of the head tracking

#### Sensitivity Step
This is used in coordination with the Increase and Decrease sensitivity keys

#### Increase/Decrease Sensitivity
Press either key to adjust the sensitivity multiplier by the sensitivity step value

## Troubleshooting
If the tracking is not working in game, verify the following:
- Head tracking is enabled and working properly in your head tracking application
- You are executing the game with plugins enabled using Pulsar
- The head tracking plugin is enabled and the Tracking Sensitivity is above 0.00
- The log file in `%AppData%\SpaceEngineers2\Temp\Logs` contains the line `Found shared memory map: 'FT_SharedMem'`

If you have exhausted all other options, reach out to me on Discord to submit a bug report
