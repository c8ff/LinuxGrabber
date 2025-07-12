# LinuxGrabber
A CLI PID grabber for Splatoon v288 for Cemu running on Linux.

![image](preview.png)

## Newer project - PNIDGrab
Check out [PNIDGrab](https://github.com/JerrySM64/PNIDGrab) by [Jerry Starke](https://github.com/JerrySM64)❗❗❗
It's re-implementation in Rust based on this project, with additional fixes and features (like human-readable PNIDs in the output). I strongly recommend using that project instead.

## Output
The output contains following:
- Player's index and name
- Session ID (game's identification number in hexadecimal & decimal)
- PID (in both hex and decimal formats)
- Fetch date (when the program was ran)

## Credits
LinuxGrabber uses code based from other projects, and also contributions from users. These are the credits for the main features of this project:
- [Splatheap-PID-Grabber](https://github.com/javiig8/Splatheap-PID-Grabber/blob/33166b5d679043f82b451f778b5918be228c88eb/splatheap/Form1.cs#L89) - Main PID grabbing functionality
- [Tombuntu](https://github.com/ReXiSp) - Session ID grabbing implementation & optimizing code
- [CrafterPika](https://github.com/CrafterPika) - Testing, programming help, providing useful feedback and emotional support

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

And you're done.

### Native binary

For making a native binary use the following command (after building the project):

```bash
mkbundle -o LinuxGrabber --simple LinuxGrabber.exe --no-machine-config --no-config
```

It will output a binary file for linux.

## Running the program
As this application needs to access another's process memory, it needs to be run as root or as a user that has permission to do so:

```bash
sudo mono ./LinuxGrabber
```
