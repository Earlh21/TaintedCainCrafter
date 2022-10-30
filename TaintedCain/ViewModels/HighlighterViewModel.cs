using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using TaintedCain.Models;

namespace TaintedCain.ViewModels
{
    public class HighlighterViewModel : ViewModelBase
    {
		private string filter_name = "";
		private string filter_description = "";

		public ICollection<Item> Items { get; set; }
		public Color Highlighter { get; set; }

		public string FilterName
		{
			get => filter_name;
			set
			{
				filter_name = value;
				NotifyPropertyChanged("FilterName");
				ItemsView.Refresh();
			}
		}

		public string FilterDescription
		{
			get => filter_description;
			set
			{
				filter_description = value;
				NotifyPropertyChanged("FilterDescription");
				ItemsView.Refresh();
			}
		}

		public Color[] AvailableColors { get; } =
		{
			Color.FromArgb(0, 0, 0, 0),
			Color.FromArgb(100, 140, 140, 140),
			Color.FromArgb(100, 0, 0, 0),
			Color.FromArgb(100, 255, 0, 0),
			Color.FromArgb(100, 0, 255, 0),
			Color.FromArgb(100, 0, 0, 255),
			Color.FromArgb(100, 255, 255, 0),
			Color.FromArgb(100, 255, 0, 255),
			Color.FromArgb(100, 0, 255, 255),
			Color.FromArgb(100, 255, 165, 0),
			Color.FromArgb(100, 75, 0, 130),
		};

		public RelayCommand<Item> SetItemHighlight { get; }
		public RelayCommand<Color> SetHighlighter { get; }

		public ICollectionView ItemsView { get; }

		public HighlighterViewModel(ICollection<Item> items)
        {
			SetItemHighlight = new RelayCommand<Item>(item => item.HighlightColor = Highlighter);
			SetHighlighter = new RelayCommand<Color>(color => Highlighter = color);

            Items = items;

			var items_view = new CollectionViewSource { Source = Items };

            items_view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

			ItemsView = items_view.View;
			ItemsView.Filter = obj => ItemFilter((Item)obj);
		}

		public bool ItemFilter(Item item)
		{
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
