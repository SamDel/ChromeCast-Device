# Chromecast Device

With this software you can turn a Linux device (e.g. Raspberry Pi) into a device that can be used by [Desktop Audio Streamer](https://github.com/SamDel/ChromeCast-Desktop-Audio-Streamer) to play audio. I've tested on a Raspberry Pi 2 Model B Rev 1.1 with Raspbian GNU/Linux 10 (buster).

# Install
- Download the ChromeCast.Device file from [releases](https://github.com/SamDel/ChromeCast-Device/releases)
- Copy the file to your Linux device
- Change permission: `chmod 777 ChromeCast.Device`
- Execute: `./ChromeCast.Device -l -n MyDeviceName`
- Use [Desktop Audio Streamer](https://github.com/SamDel/ChromeCast-Desktop-Audio-Streamer) on a Windows device to start streaming.

# Arguments
The command-line arguments:
- `-l` for logging
- `-n DeviceName` for the name of the device

# Utilities
Chromecast.Device uses these Linux applications, so make sure they are installed:

- vlc/cvlc to start an audio stream: `cvlc <url>`
- pkill to stop an audio stream: `pkill vlc`
- amixer for the volume:
   * `amixer get Master playback` to get the level
   * `amixer set Master <level (0-65536)>` to set the level
   * `amixer -q sset Master toggle` to toggle mute

# Dependencies
- [Makaretu.Dns.Multicast](https://github.com/richardschneider/net-mdns)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)

