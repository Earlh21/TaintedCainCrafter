using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using TaintedCain.Models;

namespace TaintedCain.ViewModels
{
    public class ItemViewModel : ViewModelBase
    {
        public Action CloseAction;

        private ObservableCollection<Pickup> BlacklistedPickups { get; } = new ObservableCollection<Pickup>();

        public Item Item { get; set; }
        public ItemManager ItemManager { get; set; }

        public Tuple<Item, Recipe>? Result { get; private set; }

        public RelayCommand<Recipe> CraftItem { get; }
        public RelayCommand<Recipe> PlanItem { get; }
        public RelayCommand<Pickup> BlacklistPickup { get; }

        public ICollectionView ItemsView { get; }

        public ItemViewModel(Item item, ItemManager item_manager, Action close_action)
        {
            CloseAction = close_action;

            CraftItem = new RelayCommand<Recipe>(recipe =>
            {
                ItemManager.RemovePickups(recipe.Pickups);
                CloseAction();
            });

            PlanItem = new RelayCommand<Recipe>(recipe =>
            {
                ItemManager.RemovePickups(recipe.Pickups);
                Result = new Tuple<Item, Recipe>(Item, recipe);
                CloseAction();
            });

            BlacklistPickup = new RelayCommand<Pickup>(pickup => BlacklistedPickups.Add(pickup.Copy()));

            Item = item;
            ItemManager = item_manager;

            var items_view = new CollectionViewSource() { Source = Item.Recipes };

            items_view.SortDescriptions.Add(new SortDescription("AverageQuality", ListSortDirection.Ascending));
            items_view.IsLiveSortingRequested = true;

            ItemsView = items_view.View;
            ItemsView.Filter = obj => RecipeFilter((Recipe)obj);

            BlacklistedPickups.CollectionChanged += (sender, e) => ItemsView.Refresh();
        }

        public bool RecipeFilter(Recipe recipe)
        {
            foreach (var pickup in BlacklistedPickups)
            {
                if (recipe.Pickups.Any(p => p.Id == pickup.Id))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
