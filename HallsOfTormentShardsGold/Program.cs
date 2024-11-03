using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll")]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

    const int PROCESS_WM_READ = 0x0010;
    const int PROCESS_VM_WRITE = 0x0020;
    const int PROCESS_VM_OPERATION = 0x0008;

    static IntPtr GetFinalAddress(IntPtr processHandle, long baseAddress, int[] offsets)
    {
        IntPtr address = (IntPtr)baseAddress;
        byte[] buffer = new byte[8];
        IntPtr bytesRead;

        foreach (int offset in offsets)
        {
            ReadProcessMemory(processHandle, address, buffer, sizeof(long), out bytesRead);
            address = (IntPtr)(BitConverter.ToInt64(buffer, 0) + offset);
        }

        return address;
    }

    static long ReadValue(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[8];
        ReadProcessMemory(processHandle, address, buffer, sizeof(long), out IntPtr bytesRead);
        return BitConverter.ToInt64(buffer, 0);
    }

    static void WriteValue(IntPtr processHandle, IntPtr address, long value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        WriteProcessMemory(processHandle, address, buffer, sizeof(long), out IntPtr bytesWritten);
    }

    static void Main(string[] args)
    {
        Process[] processes = Process.GetProcessesByName("HallsOfTorment");
        if (processes.Length == 0)
        {
            Console.WriteLine("Game not found!");
            Console.ReadLine();
            return;
        }

        Process gameProcess = processes[0];
        IntPtr processHandle = OpenProcess(PROCESS_WM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, gameProcess.Id);
        long baseAddress = gameProcess.MainModule.BaseAddress.ToInt64() + 0x039495E8; // For version 2024-10-11

        var goldOffsets = new int[] { 0x68, 0x28, 0x140, 0x18, 0x20, 0x28 };
        var shardsOffsets = new int[] { 0x68, 0x28, 0x140, 0x18, 0x168, 0x30 };

        IntPtr goldAddress = (IntPtr)(GetFinalAddress(processHandle, baseAddress, goldOffsets).ToInt64() + 0x8); //our pointer to gold is not directly at the gold value, so we need to add 0x8 to get the correct address
        IntPtr shardsAddress = GetFinalAddress(processHandle, baseAddress, shardsOffsets);

        while (true)
        {
            Console.WriteLine("\n================Halls of Torment - Gold & Torment Shards Editor================");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("!!!!! MAKE SURE YOU ARE IN GAME WITH A CHARACTER AND PAUSED. OTHERWISE WON'T WORK !!!!!");
            Console.WriteLine("\n1. View Gold");
            Console.WriteLine("2. Write Gold (0-9999999)");
            Console.WriteLine("3. View Torment Shards");
            Console.WriteLine("4. Write Torment Shards (1-10000)");
            Console.WriteLine("5. Exit");
            Console.WriteLine("Choose option: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.WriteLine($"Current Gold: {ReadValue(processHandle, goldAddress)}");
                    break;

                case "2":
                    Console.Write("Enter new Gold value (0-9999999): ");
                    if (long.TryParse(Console.ReadLine(), out long newGold) && newGold >= 0 && newGold <= 9999999)
                    {
                        WriteValue(processHandle, goldAddress, newGold);
                        Console.WriteLine("Gold value written successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Invalid value! Must be between 0 and 9999999");
                    }
                    break;

                case "3":
                    Console.WriteLine($"Current Torment Shards: {ReadValue(processHandle, shardsAddress)}");
                    break;

                case "4":
                    Console.Write("Enter new Torment Shards value (1-10000): ");
                    if (long.TryParse(Console.ReadLine(), out long newShards) && newShards >= 1 && newShards <= 10000)
                    {
                        WriteValue(processHandle, shardsAddress, newShards);
                        Console.WriteLine("Torment Shards value written successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Invalid value! Must be between 1 and 10000");
                    }
                    break;

                case "5":
                    return;

                default:
                    Console.WriteLine("Invalid option!");
                    break;
            }
        }
    }
}