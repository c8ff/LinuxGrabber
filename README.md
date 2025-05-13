# LinuxGrabber
A CLI PID grabber for Splatoon v288 for Cemu running on Linux.

## Output
The output contains following:
- Player's index and name
- PID (in both hex and decimal formats)
- Fetch date (when the program was ran)

![image](preview.png)

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
