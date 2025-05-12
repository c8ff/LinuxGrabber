# LinuxGrabber
A PID grabber for Splatoon v288 for Cemu running on Linux.

## Compile
You'll need the mono package to compile the program. I developed this application on Arch linux, so I installed mono using:

"sudo pacman -S mono"

To compile the program simply run:
"mcs -out:LinuxGrabber LinuxGrabber.cs"

And you have the executable.

## Running the program
As this application needs to access another's process memory, it needs to be run as root:
"sudo ./LinuxGrabber"