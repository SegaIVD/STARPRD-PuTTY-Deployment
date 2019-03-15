using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace PuTTY_Configurator
{
    class Program
    {
        static int numerOfNics = 0;
        static List<string> macAddrs = new List<string>();
        static void Main(string[] args)
        {
            Console.WriteLine("Press Enter to add STARPRD into registry");
            Console.ReadLine();
            Console.WriteLine("Addidng STARPRD session with Answerback to PuTTY\n");
            ShowNetworkInterfaces();
            Console.Write("\nEnter the interface number for Answerback (1-{0}): ", macAddrs.Count);
            
            int selection = Int32.Parse(Console.ReadLine());
            Console.WriteLine("Answerback: " + macAddrs[selection - 1]);
            Console.WriteLine("Addidng STARPRD session to PuTTY\n");
            File.WriteAllText(Path.GetTempPath() + "\\STARPRD.reg", Resource1.STARPRD);
            Process regeditProcess = Process.Start("regedit.exe", " /s " + Path.GetTempPath() + "\\STARPRD.reg");
            regeditProcess.WaitForExit();

            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\SimonTatham\PuTTY\Sessions\STARPRD");
            key.SetValue("Answerback", macAddrs[selection - 1]);
            key.Close();
            Console.WriteLine("The Answerback key has been added to Registry");
            File.Delete(Path.GetTempPath() + "\\STARPRD.reg");
            Console.WriteLine("\nPress enter to exit...");
            Console.ReadLine();
        }

        public static void ShowNetworkInterfaces()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            Console.WriteLine("Interface information for {0}.{1}     ",
                    computerProperties.HostName, computerProperties.DomainName);
            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("  No network interfaces found.");
                return;
            }

            Console.WriteLine("  Number of interfaces .................... : {0}", nics.Length);
            numerOfNics = nics.Length;
            int num = 0;
            foreach (NetworkInterface adapter in nics)
            {
                num++;
                IPInterfaceProperties properties = adapter.GetIPProperties(); //  .GetIPInterfaceProperties();
                Console.WriteLine();
                Console.WriteLine(num + ") " + adapter.Description);
                Console.WriteLine(String.Empty.PadLeft(adapter.Description.Length, '='));
                Console.WriteLine("  Interface type .......................... : {0}", adapter.NetworkInterfaceType);
                Console.Write("  Physical address ........................ : ");
                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
                StringBuilder mac = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    // Display the physical address in hexadecimal.
                    Console.Write("{0}", bytes[i].ToString("X2"));
                    mac.Append(bytes[i].ToString("X2"));
                    // Insert a collumn after each byte, unless we are at the end of the 
                    // address.
                    if (i != bytes.Length - 1)
                    {
                        Console.Write(":");
                    }
                }
                macAddrs.Add(mac.ToString());
                Console.WriteLine();
            }
        }
    }
}
