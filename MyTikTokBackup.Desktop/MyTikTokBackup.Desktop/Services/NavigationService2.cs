using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace MyTikTokBackup.Desktop.Services
{
    public class NavigationService2 : INavigationService
    {
        private Dictionary<string, Registration> _register = new Dictionary<string, Registration>();
        protected Frame _contentFrame;

        public NavigationService2()
        {
        }

        public void Init(Frame contentFrame)
        {
            _contentFrame = contentFrame;
        }

        public void Register<TView>() where TView : Page
        {
            var key = typeof(TView).Name;
            _register.TryAdd(key, new Registration(typeof(TView)));
        }

        public void Register<TView, TViewModel>() where TView : Page
        {
            var key1 = typeof(TView).Name;
            var key2 = typeof(TViewModel).Name;
            _register.TryAdd(key1, new Registration(typeof(TView), typeof(TViewModel)));
            _register.TryAdd(key2, new Registration(typeof(TView), typeof(TViewModel)));
        }

        public void GoToNew(string name)
        {
            GoToNew(name, new Dictionary<string, string>());
        }

        public void GoToNew(string name, Dictionary<string, string> navigationArgs)
        {
            if (_register.ContainsKey(name))
            {
                var registration = _register[name];
                _contentFrame.Navigate(registration.ViewType, navigationArgs);
            }
        }

        public bool CanGoBack => _contentFrame.CanGoBack;

        public void GoBack()
        {
            if (CanGoBack)
            {
                _contentFrame.GoBack();
            }
        }

        public void SetViewDataContext(Page view, object viewModel)
        {
            view.DataContext = viewModel;
        }
    }
}
