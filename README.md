# Space Engineers 2 Head Tracking Plugin
This plugin enables head tracking in Space Engineers 2 using the FreeTrack protocol.  Head tracking applies to the cockpit, on-foot, and ship external cameras.  Cockpit is enabled by default, the other two can be enabled in settings and configured individually

## Prerequisites
- [OpenTrack](https://github.com/opentrack/opentrack) or another app that uses the FreeTrack protocol
- [Space Engineers 2](https://store.steampowered.com/app/1133870/Space_Engineers_2/)
- [Pulsar](https://github.com/SpaceGT/Pulsar)

## Configuration
Configuration is handled in the Plugins section of the SE2 application:
<img width="1521" height="3774" alt="image" src="https://github.com/user-attachments/assets/d513159e-efd0-4d85-914c-5aaeb4ff2733" />

#### Tracking Sensitivity
Use this to adjust the sensitivity of the head tracking, separate values for cockpit, on foot, and external view

#### Sensitivity Step
This is used in coordination with the Increase and Decrease sensitivity keys, each time a key is pressed, the global sensitivity will be adjusted by this amount

#### Increase/Decrease Sensitivity
Press either key to adjust the sensitivity amount by the sensitivity step value

## Troubleshooting
### First Steps
If the tracking is not working in game, verify the following:
- Head tracking is enabled and working properly in your head tracking application
- You are executing the game with plugins enabled using Pulsar
- The head tracking plugin is enabled and the Tracking Sensitivity is above 0.00
- The log file in `%AppData%\SpaceEngineers2\Temp\Logs` contains the line `Found shared memory map: 'FT_SharedMem'`

### Debugging
- The Debugging section will log additional debugging information to the game logs in `%appdata%\SpaceEngineers2\Temp\Logs` when enabled

If you have exhausted all other options, reach out to me on Discord to submit a bug report
