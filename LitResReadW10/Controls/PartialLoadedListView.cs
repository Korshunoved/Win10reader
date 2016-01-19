using System;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace LitRes.Controls
{
	public class PartialLoadedListView : ListView
	{
		private bool _alreadyHookedScrollEvents;

		public event EventHandler LoadMore;

		public PartialLoadedListView()
		{
			Loaded += PartialLoadedListBox_Loaded;
		}

		protected virtual void OnLoadMore()
		{
			EventHandler handler = LoadMore;
		    handler?.Invoke(this, EventArgs.Empty);
		}

	    private ScrollViewer sv;

        private void PartialLoadedListBox_Loaded(object sender, RoutedEventArgs e)
		{
			if (_alreadyHookedScrollEvents)
			{
				return;
			}

			_alreadyHookedScrollEvents = true;
           
			 sv = (ScrollViewer)FindElementRecursive(this, typeof(ScrollViewer)); 

			if (sv != null)
			{
                sv.ViewChanged += Sv_ViewChanged;
                
    //            // Visual States are always on the first child of the control template 
    //            FrameworkElement element = VisualTreeHelper.GetChild(sv, 0) as FrameworkElement;
				//if (element != null)
				//{
				//	var groups = VisualStateManager.GetVisualStateGroups(element);
				//	VisualStateGroup vgroup = groups.Cast<VisualStateGroup>().FirstOrDefault(@group => @group.Name == "VerticalCompression");
				//	if (vgroup != null)
				//	{
				//		vgroup.CurrentStateChanging += vgroup_CurrentStateChanging;
				//	}
				//}
			}
		}

        private void Sv_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            //var myscrollbarScrollViewer = sender as ScrollViewer;

            //if (myscrollbarScrollViewer != null)
            //{
            var verticalOffsetValue = sv.VerticalOffset;
            var maxVerticalOffsetValue = sv.ExtentHeight - sv.ViewportHeight;
            if (maxVerticalOffsetValue < 0 || Math.Abs(verticalOffsetValue - maxVerticalOffsetValue) < 1.0)
            {
                Debug.WriteLine("Scroll offset: {0}", Math.Abs(verticalOffsetValue - maxVerticalOffsetValue), null);
                OnLoadMore();
            }
            //}
        }

        private UIElement FindElementRecursive(FrameworkElement parent, Type targetType)
		{
			int childCount = VisualTreeHelper.GetChildrenCount(parent);
			UIElement returnElement = null;
			if (childCount > 0)
			{
				for (int i = 0; i < childCount; i++)
				{
					Object element = VisualTreeHelper.GetChild(parent, i);
					if (element.GetType() == targetType)
					{
						return element as UIElement;
					}
					else
					{
						returnElement = FindElementRecursive(VisualTreeHelper.GetChild(parent, i) as FrameworkElement, targetType);
					}
				}
			}
			return returnElement;
		}

		private void vgroup_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
		{
			if (e.NewState.Name == "CompressionBottom")
			{
				OnLoadMore();
			}
		}
	}
}
