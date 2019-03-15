using IWshRuntimeLibrary;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PuTTY_Deployment
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Developed by Sergei Shamsutdinov
    /// FedEx Supply Chain, 2019
    /// </summary>
    public partial class MainWindow : Window
    {
        NetworkInterface[] nics; //array for all NICs on PC
        string macAddress; //MAC address string

        public MainWindow()
        {
            InitializeComponent();
            //get computer properties
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();

            MainWindow1.Title = MainWindow1.Title + " on " + String.Format("{0}.{1}", computerProperties.HostName, computerProperties.DomainName); //change window's title text

            //get all network interfaces
            nics = NetworkInterface.GetAllNetworkInterfaces();

            //add each NIC into a drop-down list
            foreach (NetworkInterface adapter in nics)
            {
                comboBoxNIC.Items.Add(adapter.NetworkInterfaceType + " - " + adapter.Description.ToString());
            }
            comboBoxNIC.SelectedIndex = 0;
        }

        //ComboBox event
        private void ComboBoxNIC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            macAddress = nics[comboBoxNIC.SelectedIndex].GetPhysicalAddress().ToString(); //get MAC address
            textBoxMAC.Text = macAddress;
        }

        //'Deploy' button event
        private void ButtonDeploy_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder("The following sessions have been added to PuTTY: \n"); //create a new string for message
            bool IsAdded = false;
            if (checkBoxPrd.IsChecked.Value)
            {
                CreateSession(1, macAddress, checkBoxShortcut.IsChecked.Value); //create session
                stringBuilder.AppendLine("STARPRD");
                IsAdded = true;
            }
            if (checkBoxQa.IsChecked.Value)
            {
                CreateSession(2, macAddress, checkBoxShortcut.IsChecked.Value);
                stringBuilder.AppendLine("STARQA");
                IsAdded = true;
            }
            if (checkBoxStg.IsChecked.Value)
            {
                CreateSession(3, macAddress, checkBoxShortcut.IsChecked.Value);
                stringBuilder.AppendLine("STARSTG");
                IsAdded = true;
            }
            if (checkBoxTst.IsChecked.Value)
            {
                CreateSession(4, macAddress, checkBoxShortcut.IsChecked.Value);
                stringBuilder.AppendLine("STARTST");
                IsAdded = true;
            }

            //show message box
            if (IsAdded)
            {
                //show added environments
                MessageBox.Show(stringBuilder.ToString(), "Answerback: " + macAddress, MessageBoxButton.OK, MessageBoxImage.Information);
            } else
            {
                //if nothing selected, show warning
                MessageBox.Show("Select any enviroments", "PuTTY Sessions", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //add session key into Windows Registry
        private void CreateSession(int envType, string answerback, bool shortcut)
        {
            //envType - Numeric Code for WMS Environment
            //answerback - MAC addr
            //shortcut on Desktop = true/false
            string envName, registryFileText;

            //compile text for a new .reg file and create shortcuts for selected environments
            switch (envType)
            {
                case 1:
                    envName = "STARPRD"; // environment name
                    registryFileText = Resource1.STARPRD; //get STARPRD regfile from resources
                    if (shortcut)
                    {
                        CreateShortcut(envName, rbPublic.IsChecked.Value); //create PuTTY public/personal shortcut for STARPRD
                    }
                    break;
                case 2:
                    envName = "STARQA";
                    registryFileText = Resource1.STARQA;
                    if (shortcut)
                    {
                        CreateShortcut(envName, rbPublic.IsChecked.Value);
                    }
                    break;
                case 3:
                    envName = "STARSTG";
                    registryFileText = Resource1.STARSTG;
                    if (shortcut)
                    {
                        CreateShortcut(envName, rbPublic.IsChecked.Value);
                    }
                    break;
                case 4:
                    envName = "STARTST";
                    registryFileText = Resource1.STARTST;
                    if (shortcut)
                    {
                        CreateShortcut(envName, rbPublic.IsChecked.Value);
                    }
                    break;
                default:
                    return;
            }
            System.IO.File.WriteAllText(System.IO.Path.GetTempPath() + "\\" + envName + ".reg", registryFileText); //create .reg temp file and write all text into it

            //import .reg file into the registry
            Process regeditProcess = Process.Start("regedit.exe", " /s " + System.IO.Path.GetTempPath() + "\\" + envName + ".reg"); 
            regeditProcess.WaitForExit();

            //change Answerback regkey for the required environment
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\SimonTatham\PuTTY\Sessions\" + envName);
            key.SetValue("Answerback", answerback); //set key
            key.Close();
            //remove .reg temp file
            System.IO.File.Delete(System.IO.Path.GetTempPath() + "\\" + envName + ".reg");
        }

        //create shorcut on Public/User Desktop
        private void CreateShortcut(string env, bool IsPublic)
        {
            object shDesktop = (object)"Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress;
            if (IsPublic)
            {
                //public shortcut full path with the environment name
                shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) + @"\" + env + ".lnk";
            }
            else
            {
                //personal shortcut path
                shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\" + env + ".lnk";
            }
            //create a shortcut via WinAPI
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = env + " session";
            //shortcut.Hotkey = "None";
            shortcut.Arguments = "-load " + env;
            //set PuTTY folder
            string puttyFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Putty";

            //check its folder
            if (!System.IO.File.Exists(puttyFolder + @"\putty.exe"))
            {
                puttyFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Putty";
            }
            if (!System.IO.File.Exists(puttyFolder + @"\putty.exe"))
            {
                MessageBox.Show("PuTTY is not installed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            shortcut.WorkingDirectory = puttyFolder;
            shortcut.TargetPath = puttyFolder + @"\putty.exe";
            try
            {
                shortcut.Save();
            }
            catch (UnauthorizedAccessException) //Unable to save a shortcut, show error message
            {
                MessageBox.Show("Run as Administrator to create a shortcut on Public Desktop", "Access is denied", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //logic for Check Boxes
        private void CheckBoxAll_Checked(object sender, RoutedEventArgs e)
        {
            checkBoxPrd.IsChecked = true;
            checkBoxQa.IsChecked = true;
            checkBoxStg.IsChecked = true;
            checkBoxTst.IsChecked = true;
            checkBoxShortcut.IsChecked = true;
        }

        private void CheckBoxAll_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBoxPrd.IsChecked = false;
            checkBoxQa.IsChecked = false;
            checkBoxStg.IsChecked = false;
            checkBoxTst.IsChecked = false;
            checkBoxShortcut.IsChecked = false;
        }

        private void CheckBoxShortcut_Checked(object sender, RoutedEventArgs e)
        {
            rbPublic.IsEnabled = true;
            rbUser.IsEnabled = true;
        }

        private void CheckBoxShortcut_Unchecked(object sender, RoutedEventArgs e)
        {
            rbPublic.IsEnabled = false;
            rbUser.IsEnabled = false;
        }
    }
}
