using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaintedCain.Models
{
    public class Item : INotifyPropertyChanged
    {
        private int id;
        private int quality;
        private string name;
        private string description;
        private string? mod;

        private BitmapImage image;

        private bool is_blacklisted;
        private Color highlight_color = Color.FromArgb(0, 0, 0, 0);

        private ObservableCollection<Recipe> recipes = new ObservableCollection<Recipe>();

        public event PropertyChangedEventHandler PropertyChanged;

        public string? Mod
        {
            get => mod;
            set
            {
                mod = value;
                NotifyPropertyChanged("Mod");
            }
        }

        public int Id
        {
            get => id;
            set
            {
                id = value;
                NotifyPropertyChanged("Id");
            }
        }

        public int Quality
        {
            get => quality;
            set
            {
                quality = value;
                NotifyPropertyChanged("Quality");
            }
        }

        public BitmapImage Image
        {
            get => image;

            private set
            {
                image = value;
                NotifyPropertyChanged("Image");
            }
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Description
        {
            get => description;
            set
            {
                description = value;
                NotifyPropertyChanged("Description");
            }
        }

        public Color HighlightColor
        {
            get => highlight_color;
            set
            {
                highlight_color = value;
                NotifyPropertyChanged("HighlightColor");
                NotifyPropertyChanged("HasHighlight");
            }
        }

        //Just need to ensure each color has a unique place in ordering
        public int HighlightOrder => 0 | highlight_color.R << 8 | highlight_color.G << 8 | highlight_color.B;

        public bool IsBlacklisted
        {
            get => is_blacklisted;
            set
            {
                is_blacklisted = value;
                NotifyPropertyChanged("IsBlacklisted");
            }
        }

        public bool HasRecipes => Recipes.Count > 0;

        public ObservableCollection<Recipe> Recipes
        {
            get => recipes;
            set
            {
                recipes = value;
                NotifyPropertyChanged("Recipes");
            }
        }

        public Item(int id, string name, string description, string? mod, int quality, string? image_path)
        {
            Id = id;
            Name = name;
            Description = description;
            Quality = quality;
            Mod = mod;

            if (image_path == null || !File.Exists(image_path))
            {
                Image = null;
            }
            else
            {
                Image = new BitmapImage(new Uri(image_path));
            }

            Recipes.CollectionChanged += (sender, args) => { NotifyPropertyChanged("HasRecipes"); };
        }

        protected void NotifyPropertyChanged(string property_name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        }

        public bool CanCraft(ICollection<Pickup> available_pickups)
        {
            foreach (var recipe in recipes)
            {
                if (recipe.CanCraft(available_pickups))
                {
                    return true;
                }
            }

            return false;
        }
    }
}