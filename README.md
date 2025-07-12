# LinuxGrabber
A CLI PID grabber for Splatoon v288 for Cemu running on Linux.

![image](preview.png)

## PNIDGrab
Check out [PNIDGrab](https://github.com/JerrySM64/PNIDGrab) by [Jerry Starke](https://github.com/JerrySM64)!
It's re-implementation in Rust based on this project, with additional fixes and features (like human-readable PNIDs).

## Output
The output contains following:
- Player's index and name
- PID (in both hex and decimal formats)
- Fetch date (when the program was ran)

## Credits
LinuxGrabber uses code based from other projects, and also contributions from users. These are the credits for the main features of this project:
- [Splatheap-PID-Grabber](https://github.com/javiig8/Splatheap-PID-Grabber/blob/33166b5d679043f82b451f778b5918be228c88eb/splatheap/Form1.cs#L89) - Main PID grabbing functionality
- [Tombuntu](https://github.com/ReXiSp) - Session ID grabbing implementation
- [CrafterPika](https://github.com/CrafterPika) - Testing, programming help, providing useful feedback & cool guy

## Compile
You'll need the mono package to compile the program. 

For Arch linux based distros:

```bash
sudo pacman -S mono
```

For debian based distros:

```bash
sudo apt install mono-mcs
```

To compile the program simply run:
```bash
mcs -out:LinuxGrabber LinuxGrabber.cs
```

And you have the executable.

## Running the program
As this application needs to access another's process memory, it needs to be run as root or as a user that has permission to do so:

```bash
sudo mono ./LinuxGrabber
```
