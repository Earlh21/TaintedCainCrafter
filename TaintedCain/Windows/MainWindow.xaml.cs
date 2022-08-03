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

namespace TaintedCain
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private string filter_name = "";
		private string filter_description = "";

		public static ItemManager ItemManager { get; } = new ItemManager();

		public static ObservableCollection<Tuple<Item, Recipe>> PlannedRecipes { get; set; } =
			new ObservableCollection<Tuple<Item, Recipe>>();

		private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Resources\\Data\\";
		private static readonly string BlacklistPath = DataFolder + "blacklist.json";
		private static readonly string HighlightsPath = DataFolder + "highlight.json";
		private static readonly string SettingsPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";

		private UserSettings user_settings;

		public string FilterName
		{
			get => filter_name;
			set
			{
				filter_name = value;
				((CollectionViewSource) Resources["ItemsView"]).View.Refresh();
			}
		}

		public string FilterDescription
		{
			get => filter_description;
			set
			{
				filter_description = value;
				((CollectionViewSource) Resources["ItemsView"]).View.Refresh();
			}
		}
		
		public RelayCommand SetSeed { get; set; } = new RelayCommand((sender) =>
		{
			var dialog = new SeedWindow();
			var vm = dialog.DataContext as SeedViewModel;
			vm.OnRequestClose += (s, e) => dialog.Close();

			dialog.ShowDialog();
				
			if (vm.DataSubmit)
			{
				ItemManager.SetSeed(vm.Seed);
			}
		});

		public MainWindow()
		{
			if (File.Exists(BlacklistPath))
			{
				List<int> blacklisted_ids = JsonConvert.DeserializeObject<List<int>>(File.ReadAllText(BlacklistPath))
				                            ?? new List<int>();

				foreach (int id in blacklisted_ids)
				{
					Item item = ItemManager.Items.FirstOrDefault(item => item.Id == id);

					if (item != null)
					{
						item.IsBlacklisted = true;
					}
				}
			}

			uint d = Crafting.StringToSeed("JKD9Z0C9");

			if (File.Exists(HighlightsPath))
			{
				var item_highlights =
					JsonConvert.DeserializeObject<Dictionary<int, Color>>(File.ReadAllText(HighlightsPath))
					?? new Dictionary<int, Color>();

				foreach (Item item in ItemManager.Items)
				{
					item.HighlightColor = item_highlights.GetValueOrDefault(item.Id);
				}
			}

			InitializeComponent();

			if (File.Exists(SettingsPath))
			{
				user_settings = UserSettings.Load(SettingsPath);
			}
			else
			{
				user_settings = new UserSettings();
			}

			SetUiTheme(user_settings.UiTheme);
			Task.Run(CompanionServerAsync);

			//If this isn't set, the UI won't redraw when messages are received
			//while Isaac is running in fullscreen
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

			
		}

		private async Task CompanionServerAsync()
		{
			TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 12344);
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
								ItemManager.Clear();
								ItemManager.SetSeed((uint)values[0]);

								List<Pickup> pickups = new List<Pickup>();
								for (int i = 1; i < values.Count; i++)
								{
									pickups.Add(new Pickup(i, (int)values[i]));
								}

								ItemManager.SetPickups(pickups);
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
			return (CollectionView) CollectionViewSource.GetDefaultView(collection);
		}

		public void ViewItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Item item = (Item) e.Parameter;
			var window = new ItemViewWindow(item);
			window.ShowDialog();

			if (window.Result != null)
			{
				PlannedRecipes.Add(window.Result);
			}
		}

		public void ClearPickups_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			ItemManager.Clear();
		}

		public void BlacklistItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Item item = (Item) e.Parameter;
			item.IsBlacklisted = true;
		}

		public void ViewBlacklist_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var window = new BlacklistManagerWindow();
			window.ShowDialog();
		}

		public void ReleaseItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var planned = (Tuple<Item, Recipe>) e.Parameter;

			ItemManager.AddPickups(planned.Item2.Pickups);
			PlannedRecipes.Remove(planned);
		}

		public void ClearPlan_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			PlannedRecipes.Clear();
		}

		public void ClearPlan_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = PlannedRecipes.Count > 0;
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
		{
			var blacklisted_ids = ItemManager.Items
				.Where(item => item.IsBlacklisted)
				.Select(item => item.Id).ToList();

			File.WriteAllText(BlacklistPath, JsonConvert.SerializeObject(blacklisted_ids));

			var highlights = ItemManager.Items
				.ToDictionary(item => item.Id, item => item.HighlightColor);

			File.WriteAllText(HighlightsPath, JsonConvert.SerializeObject(highlights));
			user_settings.Save(SettingsPath);
		}

		//Select all text when a pickup textbox is clicked
		private void ValueText_GotFocus(object sender, RoutedEventArgs e)
		{
			TextBox tb = (TextBox) e.OriginalSource;
			tb.Dispatcher.BeginInvoke(
				new Action(delegate { tb.SelectAll(); }), System.Windows.Threading.DispatcherPriority.Input);
		}

		public void IncrementPickup_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Pickup pickup = (Pickup) e.Parameter;
			pickup.Amount++;
		}

		public void DecrementPickup_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Pickup pickup = (Pickup) e.Parameter;
			pickup.Amount--;
		}

		public void ViewHighlighter_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var window = new HighlightManagerWindow();
			window.ShowDialog();
		}

		private void ItemFilter(object sender, FilterEventArgs e)
		{
			Item item = (Item) e.Item;

			if (!item.HasRecipes)
			{
				e.Accepted = false;
				return;
			}

			if (item.IsBlacklisted)
			{
				e.Accepted = false;
				return;
			}

			if (!item.Name.ToLower().Contains(FilterName.Trim().ToLower()))
			{
				e.Accepted = false;
				return;
			}

			if (!item.Description.ToLower().Contains(FilterDescription.Trim().ToLower()))
			{
				e.Accepted = false;
				return;
			}

			e.Accepted = true;
		}

		public void ViewAbout_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var about_window = new AboutWindow();
			about_window.ShowDialog();
		}

		public void SetTheme_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			SetUiTheme((String) e.Parameter);
			user_settings.UiTheme = (String) e.Parameter;
		}

		private bool SetUiTheme(string theme)
		{
			switch (theme)
			{
				case "Dark":
					ResourceLocator.SetColorScheme(Application.Current.Resources, ResourceLocator.DarkColorScheme);
					return true;
				case "Light":
					ResourceLocator.SetColorScheme(Application.Current.Resources, ResourceLocator.LightColorScheme);
					return true;
			}

			return false;
		}

		public void Close_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}
	}

	public static class Commands
	{
		public static RoutedCommand ViewItem = new RoutedCommand("View Item", typeof(Commands));
		public static RoutedCommand CraftItem = new RoutedCommand("Craft Item", typeof(Commands));
		public static RoutedCommand ClearPickups = new RoutedCommand("Clear Pickups", typeof(Commands));
		public static RoutedCommand BlacklistItem = new RoutedCommand("Blacklist Item", typeof(Commands));
		public static RoutedCommand UnblacklistItem = new RoutedCommand("Unblacklist Item", typeof(Commands));
		public static RoutedCommand ViewBlacklist = new RoutedCommand("View Blacklist", typeof(Commands));
		public static RoutedCommand PlanItem = new RoutedCommand("Plan Item", typeof(Commands));
		public static RoutedCommand ReleaseItem = new RoutedCommand("Release Item", typeof(Commands));
		public static RoutedCommand ClearPlan = new RoutedCommand("Clear Plan", typeof(Commands));
		public static RoutedCommand IncrementPickup = new RoutedCommand("Increment Pickup", typeof(Commands));
		public static RoutedCommand DecrementPickup = new RoutedCommand("Decrement Pickup", typeof(Commands));
		public static RoutedCommand SetItemHighlight = new RoutedCommand("Set Item Highlight", typeof(Commands));
		public static RoutedCommand SetHighlighter = new RoutedCommand("Set Highlighter", typeof(Commands));
		public static RoutedCommand ViewHighlighter = new RoutedCommand("View Highlighter", typeof(Commands));
		public static RoutedCommand ViewAbout = new RoutedCommand("View About", typeof(Commands));
		public static RoutedCommand SetTheme = new RoutedCommand("Set Theme", typeof(Commands));
		public static RoutedCommand Close = new RoutedCommand("Close", typeof(Commands));
		public static RoutedCommand BlacklistPickup = new RoutedCommand("Blacklist Pickup", typeof(Commands));
	}
}