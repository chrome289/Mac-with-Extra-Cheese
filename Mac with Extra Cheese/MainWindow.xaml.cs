using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Threading;

namespace Mac_with_Extra_Cheese
{
    public partial class MainWindow : Window
    {
        public static bool is_connected = false, black = false;
        public static string adapter_lead = "00", reg_string = "", mac = "";
        public static int adapter_id = 0;
        public static ProcessStartInfo processStartInfo;
        public static Process process;
        public static List<string> list = new List<string>();
        public static string[] blacklist = { "A0-B3-CC-75-18-B0", "A0-B3-CC-7C-A1-A0", "48-D2-24-30-C3-90", "D4-BE-D9-46-C8-46" };
        public static BackgroundWorker bw = new BackgroundWorker();
        static void randomize(int n)
        {
            Random r = new Random();
            int i, ran = r.Next(100);
            for (i = n - 1; i > 0; i--)
            {
                int j = r.Next(100) % (i + 1);
                string temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
        static void restart_adapter()
        {
            processStartInfo = new ProcessStartInfo("cmd.exe", @" /c wmic path win32_networkadapter where index=" + adapter_id + " call disable");
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            process = Process.Start(processStartInfo);
            process.WaitForExit();
            processStartInfo = new ProcessStartInfo("cmd.exe", @" /c wmic path win32_networkadapter where index=" + adapter_id + " call enable");
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            process = Process.Start(processStartInfo);
            process.WaitForExit();
        }
        static void ping()
        {
            Ping ping = new Ping();
            int temp = 0;
            try
            {
                while (temp < 2)
                {
                    PingReply pingStatus = ping.Send(@"www.google.com");
                    if (pingStatus.Status == IPStatus.Success)
                    {
                        is_connected = true;
                        break;
                    }
                    else
                    {
                        is_connected = false;
                        temp++;
                    }
                }
            }
            catch (PingException e)
            {
                is_connected = false;
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            StreamReader sw1 = new StreamReader(@"c:\temp\set.txt", true);
            adapter_id=Int32.Parse(sw1.ReadLine());
            updown.Content=adapter_id;
            tb2.Content = sw1.ReadLine();
            sw1.Close();
            //MessageBox.Show(adapter_id.ToString());
            if (adapter_id < 10)
                adapter_lead = "000";
            else
                adapter_lead = "00";
            reg_string = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}\" + adapter_lead + adapter_id;
            mac = (string)Registry.GetValue(reg_string, "NetworkAddress", "none");
            l2.Content = mac;
        }

        public void prog()
        {
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerAsync();
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            l2.Content = mac;
        }
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            StreamWriter sw = new StreamWriter("c:\\temp\\log.txt", true);
            sw.AutoFlush = true;
            Console.SetOut(sw);
            while (true)
            {
                try
                {   
                    sw.Write("---------------\r\n");
                    ping();
                    if (is_connected == true)
                    {
                        sw.Write(DateTime.Now + "  You are already connected\r\n");
                        System.Threading.Thread.Sleep(120000);
                    }
                    else
                    {
                        //generate mac addresses of connected devices
                        list.Clear();
                        File.Delete(@"c:\temp\fing-log.txt");
                        Directory.SetCurrentDirectory(@"C:\Program Files (x86)\Overlook Fing 2.2\bin\");
                        processStartInfo = new ProcessStartInfo("cmd.exe", @" /c fing --silent -r 1 -o table,csv,c:\temp\fing-log.txt");
                        processStartInfo.UseShellExecute = false;
                        processStartInfo.CreateNoWindow = true;
                        process = Process.Start(processStartInfo);
                        process.WaitForExit();
                        StreamReader reader = new StreamReader(@"c:\temp\fing-log.txt");
                        string address;
                        while (reader.EndOfStream != true)
                        {
                            // parse mac address;
                            string line = reader.ReadLine();
                            string[] temp = new string[2];
                            string[] split = line.Split(":".ToCharArray(), 8);
                            split[0] = split[0].Substring(split[0].Length - 2, 2);
                            temp = split[5].Split(";".ToCharArray(), 2);
                            split[5] = temp[0];
                            temp[1] = temp[1] + "";
                            address = split[0].ToString() + "-" + split[1].ToString() + "-" + split[2].ToString() + "-" + split[3].ToString() + "-" + split[4].ToString() + "-" + split[5].ToString();

                            //Blacklist
                            black = false;
                            for (int x = 0; x < blacklist.Length; x++)
                            {
                                if (address == blacklist[x])
                                {
                                    black = true;
                                    break;
                                }
                            }

                            //check if device is a client
                            if (temp[1] != "Cisco" && black == false)
                            {
                                list.Add(address);
                            }
                        }
                        reader.Close();
                        //randomize the mac list
                        randomize(list.Count);

                        //check each mac in order
                        while (list.Count != 0)
                        {
                            address = list[0];
                            list.RemoveAt(0);

                            //change mac address
                            reg_string = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}\" + adapter_lead + adapter_id;
                            mac = (string)Registry.GetValue(reg_string, "NetworkAddress", "none");
                            Registry.SetValue(reg_string, "NetworkAddress", address);
                            mac = (string)Registry.GetValue(reg_string, "NetworkAddress", "none");

                            restart_adapter();

                            sw.Write(DateTime.Now + "  Trying MAC -- " + mac+"\r\n");

                            System.Threading.Thread.Sleep(45000);

                            ping();

                            if (is_connected == true)
                                break;
                        }
                        reader.Close();
                        BackgroundWorker worker = sender as BackgroundWorker;
                        worker.ReportProgress(0);
                        sw.Write(DateTime.Now + "  Connected\r\n");
                        sw.Write(DateTime.Now + "  Current MAC -- " + mac+"\r\n");
                        GC.Collect();
                    }
                }
                catch (Exception em)
                {
                    MessageBox.Show(DateTime.Now + "  Unhandled Exception " + em.InnerException + "    " + em.Message);
                    Environment.Exit(0);
                    sw.Close();
                }
            }
            sw.Close();
            sw.Dispose();
        }

        private void bt1_Click(object sender, RoutedEventArgs e)
        {
            prog();
            bt1.IsEnabled = false;
        }

        private void bt2_Click(object sender, RoutedEventArgs e)
        {
            
            //reset mac address
            reg_string = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}\"+adapter_lead + adapter_id;
            mac = (string)Registry.GetValue(reg_string, "NetworkAddress", "none");
            Registry.SetValue(reg_string, "NetworkAddress", tb2.Content);
            restart_adapter();
            Environment.Exit(0);
        }

    }
}