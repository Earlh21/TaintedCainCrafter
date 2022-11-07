using AdonisUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TaintedCain.Models;
using TaintedCain.Windows;

namespace TaintedCain.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public Action CloseAction { get; }

        private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Resources\\Data\\";
        private static readonly string BlacklistPath = DataFolder + "blacklist.json";
        private static readonly string HighlightsPath = DataFolder + "highlight.json";
        private static readonly string SettingsPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";

        private string filter_name = "";
        private string filter_description = "";
        private bool use_modded_items = false;
        private string mods_error_message = "";

        public string FilterName
        {
            get => filter_name;
            set
            {
                filter_name = value;
                ItemsView.Refresh();
                NotifyPropertyChanged("FilterName");
            }
        }

        public string FilterDescription
        {
            get => filter_description;
            set
            {
                filter_description = value;
                ItemsView.Refresh();
                NotifyPropertyChanged("FilterDescription");
            }
        }

        public bool UseModdedItems
        {
            get => use_modded_items;
            set
            {
                use_modded_items = value;
                NotifyPropertyChanged("UseModdedItems");
                ReloadModdedItems();
            }
        }

        public string ModsErrorMessage
        {
            get => mods_error_message;
            set
            {
                mods_error_message = value;
                NotifyPropertyChanged("ModsErrorMessage");
            }
        }

        public ItemManager ItemManager { get; } = new ItemManager();
        public UserSettings UserSettings { get; set; }

        public ObservableCollection<Tuple<Item, Recipe>> PlannedRecipes { get; set; } =
    new ObservableCollection<Tuple<Item, Recipe>>();

        public RelayCommand<Pickup> IncrementPickup { get; }
        public RelayCommand<Pickup> DecrementPickup { get; }
        public RelayCommand ClearPickups { get; }

        public RelayCommand ClearPlan { get; }
        public RelayCommand<Tuple<Item, Recipe>> ReleaseItem { get; }

        public RelayCommand<Item> BlacklistItem { get; }

        public RelayCommand<String> SetTheme { get; }
        public RelayCommand ReloadMods { get; }

        public RelayCommand SetSeed { get; }
        public RelayCommand SetModsPath { get; }
        public RelayCommand ViewAbout { get; }
        public RelayCommand ViewHighlighter { get; }
        public RelayCommand ViewBlacklist { get; }
        public RelayCommand<Item> ViewItem { get; }

        public RelayCommand<Window> Close { get; }

        public ICollectionView ItemsView { get; }

        public MainWindowViewModel(Action close_action)
        {
            CloseAction = close_action;

            IncrementPickup = new RelayCommand<Pickup>(pickup => pickup.Amount++);
            DecrementPickup = new RelayCommand<Pickup>(pickup => pickup.Amount--);

            ClearPickups = new RelayCommand(() => ItemManager.Clear());

            ClearPlan = new RelayCommand(
                () => PlannedRecipes.Clear(),
                () => PlannedRecipes.Count > 0);

            ReleaseItem = new RelayCommand<Tuple<Item, Recipe>>(planned =>
            {
                ItemManager.AddPickups(planned.Item2.Pickups);
                PlannedRecipes.Remove(planned);
            });

            BlacklistItem = new RelayCommand<Item>(item =>
            {
                item.IsBlacklisted = true;
                UserSettings.Blacklist.Add(item.Name);
            });

            SetTheme = new RelayCommand<String>(theme =>
            {
                SetUiTheme(theme);
                UserSettings.UiTheme = theme;
            });

            ReloadMods = new RelayCommand(() => ReloadModdedItems());

            SetSeed = new RelayCommand(() =>
            {
                var dialog = new SeedWindow();
                var vm = dialog.DataContext as SeedViewModel;

                dialog.ShowDialog();

                if (vm.DataSubmit)
                {
                    ItemManager.SetSeed(vm.Seed);
                }
            });

            SetModsPath = new RelayCommand(() =>
            {
                var dialog = new ModsPathWindow(UserSettings.ModsPath);
                var vm = dialog.DataContext as ModsPathViewModel;

                dialog.ShowDialog();

                if (vm.DataSubmit)
                {
                    UserSettings.ModsPath = vm.ModsPath;
                    ReloadModdedItems();
                }
            });

            ViewItem = new RelayCommand<Item>(item =>
            {
                var window = new ItemViewWindow(item, ItemManager);
                window.ShowDialog();

                var vm = window.DataContext as ItemViewModel;

                if (vm.Result != null)
                {
                    PlannedRecipes.Add(vm.Result);
                }
            });

            Close = new RelayCommand<Window>(window => CloseAction());

            ViewHighlighter = new RelayCommand(() =>
            {
                new HighlightManagerWindow(ItemManager.Items).ShowDialog();

                //Update persistent settings
                foreach (var item in ItemManager.Items)
                {
                    if (item.HighlightColor == Color.FromArgb(0, 0, 0, 0))
                    {
                        //To avoid cluttering the settings file
                        UserSettings.Highlights.Remove(item.Name);
                    }
                    else
                    {
                        UserSettings.Highlights[item.Name] = item.HighlightColor;
                    }
                }

                //Refresh sorting using new highlights
                ItemsView.Refresh();
            });

            ViewBlacklist = new RelayCommand(() =>
            {
                new BlacklistManagerWindow(ItemManager.Items).ShowDialog();

                //Update persistent settings
                foreach (var item in ItemManager.Items)
                {
                    if (!item.IsBlacklisted)
                    {
                        UserSettings.Blacklist.Remove(item.Name);
                    }
                }
            });

            ViewAbout = new RelayCommand(() => new AboutWindow().ShowDialog());

            var items_view = new CollectionViewSource();

            items_view.Source = ItemManager.Items;

            items_view.IsLiveFilteringRequested = true;
            items_view.LiveFilteringProperties.Add("HasRecipes");
            items_view.LiveFilteringProperties.Add("IsBlacklisted");

            items_view.SortDescriptions.Add(new SortDescription("HighlightOrder", ListSortDirection.Descending));
            items_view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            ItemsView = items_view.View;
            ItemsView.Filter = obj => ItemFilter((Item)obj);

            LoadSettings();
        }

        private List<Mod?> LoadModdedItems(string mods_path)
        {
            return Directory.GetDirectories(mods_path).Select(path => path + "/")
                .Where(mod_path => !File.Exists(mod_path + "disable.it"))
                .Select(mod_path => Mod.FromDirectory(mod_path))
                .Where(mod => mod != null).ToList();
        }

        public void ReloadModdedItems()
        {
            if (!UseModdedItems)
            {
                ModsErrorMessage = "";
                ItemManager.SetModdedItems(new List<Mod>());
                return;
            }

            if(UserSettings.ModsPath == null)
            {
                ModsErrorMessage = "Mods path is not set, using vanilla results";
                ItemManager.SetModdedItems(new List<Mod>());
                return;
            }

            if (!Directory.Exists(UserSettings.ModsPath))
            {
                ModsErrorMessage = "Mods path is invalid, using vanilla results";
                ItemManager.SetModdedItems(new List<Mod>());
                return;
            }

            var mod_items = LoadModdedItems(UserSettings.ModsPath);

            var invalid_mods = mod_items.Where(m => !m.IsValid).ToList();

            if (invalid_mods.Count > 0)
            {
                var invalid_mods_text = String.Join(" ", invalid_mods.Select(mod => $"'{mod.Name}'"));

                ModsErrorMessage = $"Failed to load mods: {invalid_mods_text}, results may be wrong";
            }
            else
            {
                ModsErrorMessage = "";
            }

            var valid_mods = mod_items.Where(m => m.IsValid)
                .Where(mod => mod.Items.Count > 0)
                .OrderBy(mod => mod.Name).ToList();

            ItemManager.SetModdedItems(valid_mods);
            RefreshItemSettings();
        }

        private void RefreshItemSettings()
        {
            foreach (var item in ItemManager.Items)
            {
                item.IsBlacklisted = UserSettings.Blacklist.Contains(item.Name);
                item.HighlightColor = UserSettings.Highlights.GetValueOrDefault(item.Name, Color.FromArgb(0, 0, 0, 0));
            }
        }

        private void HandleOldSettings()
        {
            //Blacklist and highlight colors used to be stored in different files and use item ids
            //If the user has those files, import them into the current settings and delete them
            if (File.Exists(BlacklistPath))
            {
                List<int> blacklisted_ids = JsonConvert.DeserializeObject<List<int>>(File.ReadAllText(BlacklistPath))
                                            ?? new List<int>();

                foreach (int id in blacklisted_ids)
                {
                    Item item = ItemManager.Items.FirstOrDefault(item => item.Id == id);

                    if (item != null)
                    {
                        UserSettings.Blacklist.Add(item.Name);
                    }
                }

                File.Delete(BlacklistPath);
            }

            if (File.Exists(HighlightsPath))
            {
                var item_highlights =
                    JsonConvert.DeserializeObject<Dictionary<int, Color>>(File.ReadAllText(HighlightsPath))
                    ?? new Dictionary<int, Color>();

                foreach (Item item in ItemManager.Items)
                {
                    var color = item_highlights.GetValueOrDefault(item.Id, Color.FromArgb(0, 0, 0, 0));

                    if (color == Color.FromArgb(0, 0, 0, 0))
                    {
                        UserSettings.Highlights.Remove(item.Name);
                    }
                    else
                    {
                        UserSettings.Highlights[item.Name] = color;
                    }
                }

                File.Delete(HighlightsPath);
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                UserSettings = UserSettings.Load(SettingsPath);
            }
            else
            {
                UserSettings = new UserSettings();
            }

            HandleOldSettings();

            RefreshItemSettings();
            SetUiTheme(UserSettings.UiTheme);
            UseModdedItems = UserSettings.UseModdedItems;

            ReloadModdedItems();
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

        public bool ItemFilter(Item item)
        {
            if (!item.HasRecipes)
            {
                return false;
            }

            if (item.IsBlacklisted)
            {
                return false;
            }

            if (!item.Name.ToLower().Contains(FilterName.Trim().ToLower()))
            {
                return false;
            }

            if (!item.Description.ToLower().Contains(FilterDescription.Trim().ToLower()))
            {
                return false;
            }

            return true;
        }
    }
}
