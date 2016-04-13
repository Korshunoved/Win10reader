using Windows.UI.Xaml;

namespace LitResReadW10.Interaction
{
    public static class ManipulationService
    {
        public static DependencyProperty ManipulationListenerProperty = DependencyProperty.RegisterAttached("ManipulationListener", typeof(ManipulationListener), typeof(ManipulationService), new PropertyMetadata(null, OnManipulationListenerChanged));

        static ManipulationService()
        {
        }

        public static ManipulationListener GetManipulationListener(UIElement element)
        {
            return (ManipulationListener)element.GetValue(ManipulationListenerProperty);
        }

        public static void SetManipulationListener(UIElement element, ManipulationListener listener)
        {
            element.SetValue(ManipulationListenerProperty, listener);
        }

        private static void OnManipulationListenerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement e1 = d as UIElement;
            if (e1 == null)
                return;
            ((ManipulationListener) e.OldValue)?.Detach(e1);
            ((ManipulationListener) e.NewValue)?.Attach(e1);
        }
    }
}
