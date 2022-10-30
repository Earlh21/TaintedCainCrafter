using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
using TaintedCain.Models;

namespace TaintedCain.ViewModels
{
    public class BlacklistViewModel : ViewModelBase
    {
        public ObservableCollection<Item> Items { get; }

        public RelayCommand<Item> UnblacklistItem { get; }

        public ICollectionView ItemsView { get; }

        public BlacklistViewModel(ICollection<Item> items)
        {
            UnblacklistItem = new RelayCommand<Item>(item => item.IsBlacklisted = false);

            Items = new ObservableCollection<Item>(items);

            var items_view = new CollectionViewSource();
            items_view.Source = Items;

            items_view.IsLiveFilteringRequested = true;
            items_view.LiveFilteringProperties.Add("IsBlacklisted");

            items_view.SortDescriptions.Add(new SortDescription("HighlightOrder", ListSortDirection.Descending));
            items_view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            ItemsView = items_view.View;
            ItemsView.Filter = obj => ItemFilter((Item)obj);
        }

        public bool ItemFilter(Item item)
        {
            return item.IsBlacklisted;
        }
    }
}
