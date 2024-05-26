using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyTikTokBackup.Core.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop.Controls
{
    public class CategorySelectionChangedEventArgs : EventArgs
    {
        public IEnumerable<ICategorySelection> SelectedCategories { get; init; }
    }

    public sealed partial class CategoriesFilterControl : UserControl
    {
        private readonly ICategoriesService _categoriesService;

        public event EventHandler<CategorySelectionChangedEventArgs> CategorySelectionChanged;

        public CategoriesFilterControl()
        {
            this.InitializeComponent();
            _categoriesService = Ioc.Default.GetService<ICategoriesService>();
            Categories = new ObservableCollection<CategoryFilter>();
            AddCategoryCommand = new AsyncRelayCommand(AddCategory);
            this.Loaded += CategoriesFilterControl_Loaded;
        }

        private async void CategoriesFilterControl_Loaded(object sender, RoutedEventArgs e)
        {
            var all = await _categoriesService.GetAll();
            foreach (var x in all.OrderBy(x => x.Name))
            {
                var categoryFilter = new CategoryFilter
                {
                    ColorHex = x.Color,
                    Name = x.Name,
                    IsSelected = false
                };
                categoryFilter.PropertyChanged += CategoryFilter_PropertyChanged;
                Categories.Add(categoryFilter);
            }
        }

        private void CategoryFilter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CategorySelectionChanged?.Invoke(this, new CategorySelectionChangedEventArgs
            {
                SelectedCategories = Categories.Where(x=>x.IsSelected).ToList()
            });
        }

        private async Task AddCategory()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName)) return;

            var newCategory = await _categoriesService.CreateCategory(NewCategoryName);
            var categoryFilter = new CategoryFilter
            {
                ColorHex = newCategory.Color,
                Name = newCategory.Name,
                IsSelected = false
            };
            categoryFilter.PropertyChanged += CategoryFilter_PropertyChanged;
            Categories.Add(categoryFilter);
        }

        public string NewCategoryName
        {
            get { return (string)GetValue(NewCategoryNameProperty); }
            set { SetValue(NewCategoryNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NewCategoryName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NewCategoryNameProperty =
            DependencyProperty.Register("NewCategoryName", typeof(string), typeof(CategoriesFilterControl), new PropertyMetadata(null));

        public ICommand AddCategoryCommand
        {
            get { return (ICommand)GetValue(AddCategoryCommandProperty); }
            set { SetValue(AddCategoryCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddCategoryCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddCategoryCommandProperty =
            DependencyProperty.Register("AddCategoryCommand", typeof(ICommand), typeof(CategoriesFilterControl), new PropertyMetadata(null));

        public IList<CategoryFilter> Categories
        {
            get { return (IList<CategoryFilter>)GetValue(CategoriesProperty); }
            set { SetValue(CategoriesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Categories.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CategoriesProperty =
            DependencyProperty.Register("Categories", typeof(IList<CategoryFilter>), typeof(CategoriesFilterControl), new PropertyMetadata(null));
    }


    public interface ICategorySelection
    {
        public bool IsSelected { get; set; }
        public string Name { get; set; }
        public string ColorHex { get; set; }
    }

    public class CategorySelection : ObservableObject, ICategorySelection
    {
        public CategorySelection(string name, string color)
        {
            Name = name;
            ColorHex = color;
            IsSelected = true;
        }

        public CategorySelection()
        {
        }

        protected bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set { SetProperty(ref isSelected, value); }
        }

        public string Name { get; set; }
        public string ColorHex { get; set; }
    }

    public class CategoryFilter : CategorySelection
    {
        new public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                SetProperty(ref isSelected, value);
                //StrongReferenceMessenger.Default.Send(new FilterByCategoryChangeMessage());
            }
        }
    }
}
