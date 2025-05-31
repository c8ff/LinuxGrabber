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

    [DllImport("libc", SetLastError = true)]
    static extern long process_vm_writev(int pid,
        IOVec[] local_iov, ulong liovcnt,
        IOVec[] remote_iov, ulong riovcnt,
        ulong flags);

    static byte[] ReadProcessMemory(int pid, ulong address, ulong length)
    {
        byte[] buffer = new byte[length];
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        try
        {
            var local = new IOVec
            {
                Base = new UIntPtr((ulong)handle.AddrOfPinnedObject().ToInt64()),
                Length = (UIntPtr)length
            };

            var remote = new IOVec
            {
                Base = (UIntPtr)address,
                Length = (UIntPtr)length
            };

            long nread = process_vm_readv(pid, new[] { local }, 1, new[] { remote }, 1, 0);
            if (nread == -1)
            {
                Console.WriteLine($"Failed to read memory. Error code: {Marshal.GetLastWin32Error()}");
                return new byte[0];
            }
            else
            {
                // Console.WriteLine($"Read {nread} bytes from process {pid} at 0x{address:X}");
            }
        }
        finally
        {
            handle.Free();
        }

        return buffer;
    }

    static bool WriteProcessMemory(int pid, ulong address, byte[] data)
    {
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

        try
        {
            var local = new IOVec
            {
                Base = new UIntPtr((ulong)handle.AddrOfPinnedObject().ToInt64()),
                Length = (UIntPtr)data.Length
            };

            var remote = new IOVec
            {
                Base = (UIntPtr)address,
                Length = (UIntPtr)data.Length
            };

            long nwritten = process_vm_writev(pid, new[] { local }, 1, new[] { remote }, 1, 0);
            if (nwritten == -1)
            {
                Console.WriteLine($"Failed to write memory. Error code: {Marshal.GetLastWin32Error()}");
                return false;
            }
            else
            {
                // Console.WriteLine($"Wrote {nwritten} bytes to process {pid} at 0x{address:X}");
                return true;
            }
        }
        finally
        {
            handle.Free();
        }
    }

    static ulong min = 0;
    static int pid = 0;

    static byte[] readBytes(uint address, uint length)
    {
        var bytes = ReadProcessMemory(pid, (ulong)(min + 0xE000000) + address - 0x10000000, length);
        return bytes;
    }

    static uint readUInt32(uint address)
    {
        var bytes = ReadProcessMemory(pid, (ulong)(min + 0xE000000) + address - 0x10000000, 4);
        Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes);
    }

    static float readFloat(uint address)
    {
        var bytes = ReadProcessMemory(pid, (ulong)(min + 0xE000000) + address - 0x10000000, 4);
        Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }

    static void writeBytes(uint address, byte[] bytes)
    {
        WriteProcessMemory(pid, (ulong)(min + 0xE000000) + address - 0x10000000, bytes);
    }

    static void writeUInt32(uint address, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Reverse(bytes);
        WriteProcessMemory(pid, (ulong)(min + 0xE000000) + address - 0x10000000, bytes);
    }

    static void writeFloat(uint address, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Reverse(bytes);
        WriteProcessMemory(pid, (ulong)(min + 0xE000000) + address - 0x10000000, bytes);
    }

    static void Main(string[] args) {
        // Ensure characters are displayed on the console properly.
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("LinuxGrabber by c8ff (Winterberry).");

        // Faster PID grab
        string[] targetNames = { "cemu", "xapfish", ".cemu-wrapped" };
        Process targetProcess = Process.GetProcesses()
            .FirstOrDefault(p => targetNames.Contains(p.ProcessName.ToLower()));

        if (targetProcess == null)
        {
            Console.WriteLine("Could not find Cemu process.");
            return;
        }

        pid = targetProcess.Id;

        // Console.WriteLine(pid);

        // Read the memory maps
        string mapsPath = $"/proc/{pid}/maps";
        string[] mapsLines = File.ReadAllLines(mapsPath);

        // BUG: If it matches from the start, it won't count it as matched, for some reason.
        byte?[] patternBytes = new byte?[] { 0x02, 0xD4, 0xE7 };

        foreach (string line in mapsLines) {
            var split = line.Split(" ");

            var addrP = split[0].Split("-");
            min = Convert.ToUInt64(addrP[0], 16);
            var max = Convert.ToUInt64(addrP[1], 16);

            if (!split[1].Contains("r")) continue; // needs to be readable
            var len = (max - min);
            if (len < 1308622848) continue; // specific to cemu

            try
            {
                // Prevent reading the whole map memory
                var bytes = ReadProcessMemory(pid, (ulong)(min + 0xE000000), 20);
                var match = FindMatch(patternBytes, bytes, (UIntPtr)(20));

                Console.WriteLine("Player X: PID (Hex)| PID (Dec)  | Name");
                Console.WriteLine("----------------------------------------------------");
                if (match != UIntPtr.Zero)
                {
                    for (var i = 0; i < 8; i++)
                    {
                        var ptrToPlayerInfo = readUInt32((uint)(readUInt32((uint)(readUInt32(0x101DD330) + 0x10)) + (uint)(i * 4)));
                        var nameBytes = readBytes(ptrToPlayerInfo + 0x6, 32);
                        var name = Encoding.BigEndianUnicode.GetString(nameBytes);
                        name = name.Replace("\n", "").Replace("\r", ""); // Remove newline characters

                        var pidRaw = readUInt32(ptrToPlayerInfo + 0xD0);
                        string nnidHex = BitConverter.ToString(BitConverter.GetBytes(pidRaw)).Replace("-", "");
                        Console.WriteLine($"Player {i}: {nnidHex} | {pidRaw} | {name}");
                    }

                    var ptr = readUInt32(0x101E8980);
                    if (ptr != 0)
                    {
                        var index = readBytes(ptr + 0xBD, 1)[0];
                        var sessionID = readUInt32(ptr + index + 0xCC);
                        Console.WriteLine($"\nSession ID: {sessionID:X8}");
                    }
                    else
                        Console.WriteLine($"\nSession ID: None");

                    string now = DateTime.Now.ToString();
                    Console.WriteLine($"\nFetched at: {now}");
                    return;
                }
            }
            catch (Exception e)
            {
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
