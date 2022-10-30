using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using AdonisUI;
using Newtonsoft.Json;
using TaintedCain.ViewModels;
using TaintedCain.Windows;
using TaintedCain.Models;

namespace TaintedCain
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Resources\\Data\\";
        private static readonly string SettingsPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";

        public MainWindow()
        {
            var vm = new MainWindowViewModel(Close);
            DataContext = vm;

            InitializeComponent();
            Task.Run(() => { _ = CompanionServerAsync(vm); });

            //If this isn't set, the UI won't redraw when messages are received
            //while Isaac is running in fullscreen
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        private async Task CompanionServerAsync(MainWindowViewModel vm)
        {
            var item_manager = vm.ItemManager;

            TcpListener server = new TcpListener(IPAddress.Loopback, 12344);
            server.Start();

            while (true)
            {
                try
                {
                    Socket client = await server.AcceptSocketAsync();
                    while (true)
                    {
                        if (client.Poll(0, SelectMode.SelectRead) && client.Available == 0)
                        {
                            break;
                        }

                        Byte[] buffer = new byte[2048];
                        client.Receive(buffer);

                        string message = System.Text.Encoding.Default.GetString(buffer.TakeWhile((b => b != '\n')).ToArray());

                        try
                        {
                            var values = message.Replace("\n", "")
                                .Replace("\r", "")
                                .Split(',')
                                .Select(p => Convert.ToUInt32(p))
                                .ToList();

                            //Must be done to update Item recipes (ObservableCollections) from another thread
                            App.Current.Dispatcher.Invoke(delegate
                            {
                                //Clear first to prevent performance drop from recalculating recipes after setting seed
                                item_manager.Clear();
                                item_manager.SetSeed((uint)values[0]);

                                List<Pickup> pickups = new List<Pickup>();
                                for (int i = 1; i < values.Count; i++)
                                {
                                    pickups.Add(new Pickup(i, (int)values[i]));
                                }

                                vm.ReloadModdedItems();
                                item_manager.SetPickups(pickups);
                            });
                        }
                        catch (FormatException)
                        {
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private static CollectionView GetDefaultView(object collection)
        {
            return (CollectionView)CollectionViewSource.GetDefaultView(collection);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            var vm = (MainWindowViewModel)DataContext;

            var user_settings = vm.UserSettings;

            user_settings.UseModdedItems = vm.UseModdedItems;
            user_settings.Save(SettingsPath);
        }

        //Select all text when a pickup textbox is clicked
        private void ValueText_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)e.OriginalSource;
            tb.Dispatcher.BeginInvoke(
                new Action(delegate { tb.SelectAll(); }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }
}