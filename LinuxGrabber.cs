/*
 * LinuxGrabber
 * Copyright (C) 2025 c8ff
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

/*
 * The core functionality was taken from Principal_IDGrabber.
 * https://github.com/javiig8/Splatheap-PID-Tool/blob/master/PrincipalID_Grabber/Form1.cs
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

public static class AAA {
    public static byte[] GetRange(this byte[] src, int start, int len) {
        var dest = new byte[len];
        Array.Copy(src, start, dest, 0, len);
        return dest;
    }
}

class LinuxGrabber {
    [StructLayout(LayoutKind.Sequential)]
    struct IOVec {
        public UIntPtr Base;
        public UIntPtr Length;
    }

    [DllImport("libc", SetLastError = true)]
    static extern long process_vm_readv(int pid,
        IOVec[] local_iov, ulong liovcnt,
        IOVec[] remote_iov, ulong riovcnt,
        ulong flags);

    static byte[] ReadProcessMemory(int pid, ulong address, ulong length) {
        byte[] buffer = new byte[length];
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        try {
            var local = new IOVec {
                Base = new UIntPtr((ulong)handle.AddrOfPinnedObject().ToInt64()),
                Length = (UIntPtr)length
            };

            var remote = new IOVec {
                Base = (UIntPtr)address,
                Length = (UIntPtr)length
            };

            long nread = process_vm_readv(pid, new[] { local }, 1, new[] { remote }, 1, 0);
            if (nread == -1) {
                Console.WriteLine($"Failed to read memory. Error code: {Marshal.GetLastWin32Error()}");
                return new byte[0];
            } else {
                Console.WriteLine($"Read {nread} bytes from process {pid} at 0x{address:X}");
            }
        } finally {
            handle.Free();
        }

        return buffer;
    }

    static void Main(string[] args) {
        // Ensure characters are displayed on the console properly.
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("LinuxGrabber by c8ff (Winterberry).");

        string processName = null;
        Process[] processes = Process.GetProcesses();
        foreach (Process process in processes) {
            if (process.ProcessName.ToLower() == "cemu" || process.ProcessName.ToLower() == "xapfish" || process.ProcessName.ToLower()  == ".cemu-wrapped") {
                processName = Convert.ToString(process.ProcessName);
            }
        }
        if (processName == null) {
            Console.WriteLine("Could not find Cemu process.");
            return;
        }

        Process targetProcess = Process.GetProcessesByName(processName).FirstOrDefault();
        if (targetProcess == null) {
            Console.WriteLine("Target process not found.");
            return;
        }

        int pid = targetProcess.Id;

        // Read the memory maps
        string mapsPath = $"/proc/{pid}/maps";
        string[] mapsLines = File.ReadAllLines(mapsPath);

        // BUG: If it matches from the start, it won't count it as matched, for some reason.
        byte?[] patternBytes = new byte?[]{0x02, 0xD4, 0xE7};

        foreach (string line in mapsLines) {
            var split = line.Split(" ");

            var addrP = split[0].Split("-");
            var min = Convert.ToUInt64(addrP[0], 16);
            var max = Convert.ToUInt64(addrP[1], 16);

            if (!split[1].Contains("r")) continue; // needs to be readable
            var len = (max - min);
            if (len < 1308622848) continue; // specific to cemu

            try {
                // TODO: Prevent reading the whole map memory
                var bytes = ReadProcessMemory(pid, (ulong)(min + 0xE000000), (ulong)(len - 0xE000000));
                var match = FindMatch(patternBytes, bytes, (UIntPtr)(len - 0xE000000));

                Console.WriteLine("Player X: PID (Hex)| PID (Dec)  | Name");
                Console.WriteLine("----------------------------------------------------");
                if (match != UIntPtr.Zero) {
                    for (var i = 0; i < 8; i++) {
                        var p =  BitConverter.ToInt32(bytes.GetRange(-0x10000000 + 0x101DD330, 4).Reverse().ToArray(), 0);
                        var p1 = BitConverter.ToInt32(bytes.GetRange(-0x10000000 + (int) (p + 0x10), 4).Reverse().ToArray(), 0);
                        var p2 = BitConverter.ToInt32(bytes.GetRange(-0x10000000 + (int) (p1 + i * 4), 4).Reverse().ToArray(), 0);
                        var p3 =                      bytes.GetRange(-0x10000000 + (int) (p2 + 0xd0), 4);

                        var nameBytes = bytes.GetRange(-0x10000000 + (int) (p2 + 0x6), 40);
                        var name = Encoding.BigEndianUnicode.GetString(nameBytes);
                        name = name.Replace("\n", "").Replace("\r", ""); // Remove newline characters

                        string nnidHex = BitConverter.ToString(p3).Replace("-", "");
                        int nnidDec = BitConverter.ToInt32(p3.Reverse().ToArray(), 0);
                        Console.WriteLine($"Player {i}: {nnidHex} | {nnidDec} | {name}");
                    }

                    string now = DateTime.Now.ToString();
                    Console.WriteLine($"\nFetched at: {now}");
                    return;
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                break;
            }
        }

        Console.WriteLine("Unable to find game's memory address. Is the game running?");
    }

    private static UIntPtr FindMatch(byte?[] pattern, byte[] buffer, UIntPtr bytesRead) {
        int br = (int)bytesRead;
        int patternLen = pattern.Length;

        for (int i = 0, j = 0; i + patternLen <= br; i++) {
            if (pattern[j] == null || buffer[i] == pattern[j]) {
				j++;
            } else {
				j = 0;
            }

			if (j >= patternLen) {
				return (UIntPtr) (i - j + 1);
			}
        }

        return UIntPtr.Zero;
    }
}
