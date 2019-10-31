using Memory;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DarkSouls3DeathCounter {
    class Program {
        const string FileName = "DarkSouls3DeathCounter.txt";
        const int ProcessReadInterval = 1000;
        const int MemReadInterval = 100;

        static readonly Mem Memory = new Mem();

        static Task _aobScanTask;
        static long _deaths = -1;
        static string _deathPtr = string.Empty;

        // ========================================================================
        static void Main() {
            while(true) {
                while((Memory.theProc == null || Memory.theProc.HasExited) && !Memory.OpenProcess("DarkSoulsIII")) {
                    Console.WriteLine("Process 'DarkSoulsIII.exe' not found.");
                    Thread.Sleep(ProcessReadInterval);
                }

                if(_aobScanTask == null || (_aobScanTask.IsCompleted && _deathPtr == string.Empty)) {
                    _aobScanTask = ScanDeathAddressAsync();
                } else if(_aobScanTask.IsCompleted && _deathPtr != string.Empty) {
                    long newDeaths = Memory.readInt(_deathPtr);
                    if(_deaths != newDeaths) {
                        _deaths = newDeaths;
                        Console.WriteLine("Deaths = {0}", _deaths.ToString());
                        File.WriteAllText(FileName, _deaths.ToString());
                    }
                }

                Thread.Sleep(MemReadInterval);
            }
        }

        // ========================================================================
        static async Task ScanDeathAddressAsync() {
            long aobScanAddress = (await Memory.AoBScan("48 8B 05 ?? ?? ?? ?? 48 85 C0 ?? ?? 48 8B 40 ?? C3")).FirstOrDefault();
            int offset = Memory.readInt("0x" + (aobScanAddress + 3).ToString("X"));
            long heroAddress = aobScanAddress + offset + 7;
            _deathPtr = heroAddress.ToString("X") + ",0x98";
        }
    }
}
