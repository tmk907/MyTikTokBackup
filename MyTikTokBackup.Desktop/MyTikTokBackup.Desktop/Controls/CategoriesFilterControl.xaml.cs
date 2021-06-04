using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyTikTokBackup.Desktop.Controls
{
    public sealed partial class CategoriesFilterControl : UserControl
    {
        public CategoriesFilterControl()
        {
            this.InitializeComponent();
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

    public class CategorySelection : ObservableObject
    {
        public CategorySelection(string name, string color)
        {
            Name = name;
            Color = color;
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
        public string Color { get; set; }
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
