using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MyTikTokBackup.Desktop.Services
{
    public interface INavigationService
    {
        bool CanGoBack { get; }

        void GoBack();
        void GoToNew(string name);
        void GoToNew(string name, Dictionary<string, string> navigationArgs);
        void Init(Frame contentFrame);
        void Register<TView>() where TView : Page;
        void Register<TView, TViewModel>() where TView : Page;
    }

    public class NavigationStackElement
    {
        public NavigationStackElement()
        {

        }

        public string Name { get; set; }
        public object ViewModel { get; set; }
        public Dictionary<string, string> NavigationArgs { get; set; }
        public bool FullScreen { get; set; }
    }

    //public class NavigationService : INavigationService
    //{
    //    private Dictionary<Type, object> _singletonViewModels = new Dictionary<Type, object>();
    //    private Dictionary<string, Registration> _register = new Dictionary<string, Registration>();

    //    protected Stack<NavigationStackElement> _navigationStack = new Stack<NavigationStackElement>();

    //    protected Frame _contentFrame;

    //    public NavigationService()
    //    {
    //    }

    //    public void Init(Frame contentFrame)
    //    {
    //        _contentFrame = contentFrame;
    //    }

    //    public void Register<TView>() where TView : Page
    //    {
    //        var key = typeof(TView).Name;
    //        _register.Add(key, new Registration(typeof(TView)));
    //    }

    //    public void Register<TView, TViewModel>() where TView : Page
    //    {
    //        var key1 = typeof(TView).Name;
    //        var key2 = typeof(TViewModel).Name;
    //        _register.Add(key1, new Registration(typeof(TView), typeof(TViewModel)));
    //        _register.Add(key2, new Registration(typeof(TView), typeof(TViewModel)));
    //    }

    //    public void GoToNew(string name)
    //    {
    //        GoToNew(name, new Dictionary<string, string>());
    //    }

    //    public object CurrentViewModel => _navigationStack.Peek().ViewModel;

    //    public NavigationStackElement CurrentState => _navigationStack.Peek();

    //    public void GoToNew(string name, Dictionary<string, string> navigationArgs)
    //    {
    //        if (_register.ContainsKey(name))
    //        {
    //            var registration = _register[name];
    //            object viewmodel = null;
    //            if (registration.IsViewModelRegistered)
    //            {
    //                viewmodel = Ioc.Default.GetService(registration.ViewModelType);
    //            }

    //            _navigationStack.Push(new NavigationStackElement()
    //            {
    //                Name = name,
    //                ViewModel = viewmodel,
    //                NavigationArgs = navigationArgs,
    //            });
    //            _contentFrame.Navigate(registration.ViewType, navigationArgs);
    //        }
    //    }

    //    public bool CanGoBack => _navigationStack.Count > 1;

    //    public void GoBack()
    //    {
    //        if (!CanGoBack)
    //        {
    //            return;
    //        }
    //        _navigationStack.Pop();
    //        var current = _navigationStack.Peek();
    //        _contentFrame.GoBack();
    //    }

    //    public void SetViewDataContext(Page view, object viewModel)
    //    {
    //        view.DataContext = viewModel;
    //    }
    //}

    class Registration
    {
        public Registration(Type viewType)
        {
            ViewType = viewType;
        }

        public Registration(Type viewType, Type viewModelType)
        {
            ViewType = viewType;
            ViewModelType = viewModelType;
        }

        public Type ViewType { get; set; }
        public Type ViewModelType { get; set; }
        public bool IsViewModelRegistered => ViewModelType != null;
    }
}
