
using System.ComponentModel;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Digillect.Mvvm.UI;

using LitRes.ViewModels;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View("BookChapters")]
	public partial class BookChapters : BookChaptersFitting
    {
        #region Constructors/Disposer
        public BookChapters()
		{
			InitializeComponent();

            Loaded += BookChapters_Loaded;
		}

        void BookChapters_Loaded(object sender, RoutedEventArgs e)
        {
            Analytics.Instance.sendMessage(Analytics.ViewTOC);
        }
		#endregion

	    private void TockListView_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            LocalBroadcastReciver.Instance.OnPropertyChanging(TockListView.SelectedItem, new PropertyChangingEventArgs("TocTapped"));
            if(!SystemInfoHelper.IsDesktop() && Frame.CanGoBack) Frame.GoBack();

        }
	}

	public class BookChaptersFitting : EntityPage<BookChaptersViewModel.ChapterRootNode, BookChaptersViewModel>
	{
	}
}