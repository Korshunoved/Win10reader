using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm.UI;

using LitRes;
using LitRes.Models;
using LitRes.ValueConverters;
using LitRes.ViewModels;
using LitResReadW10.Controls;

namespace LitRes.Views
{
	[View("BooksByCategory")]
	[ViewParameter("category", typeof(int))]
	[ViewParameter("id", typeof(int),Required = false)]
	[ViewParameter("title", typeof(string),Required = false)]
	public partial class BooksByCategory : BooksByCategoryFitting
	{
	    private string _title;
		#region Constructors/Disposer
		public BooksByCategory()
		{
			InitializeComponent();
            Loaded += BooksByCategory_Loaded;
		}

        private void BooksByCategory_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.LoadMoreCalled.Execute(null);
        }
        #endregion

        #region CreateDataSession
        protected override Digillect.Mvvm.Session CreateDataSession( DataLoadReason reason )
		{
            int param = ViewParameters.GetValue<int>("category");
		    ViewModel.BooksViewModelType = param;
            if (ViewParameters.Contains("id"))
            {
                int id = ViewParameters.GetValue<int>("id");
                ViewModel.GenreOrTagOrSeriaID = id;
            }

            if (ViewParameters.Contains("title"))
            {
                _title = ViewParameters.GetValue<string>("title");
            }


            switch ((BooksByCategoryViewModel.BooksViewModelTypeEnum)ViewModel.BooksViewModelType)
            {
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Interesting:
                    Analytics.Instance.sendMessage(Analytics.ViewInteresting);
                    break;
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Novelty:
                    Analytics.Instance.sendMessage(Analytics.ViewNew);
                    break;
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Popular:
                    Analytics.Instance.sendMessage(Analytics.ViewPop);
                    break;
                default:
                    break;
            }

			return base.CreateDataSession( reason );
		}
		#endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!string.IsNullOrEmpty(_title))
                ControlPanel.Instance.TopBarTitle = _title;
            else
                ControlPanel.Instance.TopBarTitle =
                    (string) (new EnumCategoryTitleConverter().Convert(ViewModel.BooksViewModelType, null, null));
            //if (NavigationContext.QueryString.ContainsKey("NavigatedFrom"))
            //{
            //    if (NavigationContext.QueryString["NavigatedFrom"].Equals("toast"))
            //    {                   
            //        if (NavigationContext.QueryString.ContainsKey(PushNotificationsViewModel.SPAMPACK_TAG))
            //            PushNotificationsViewModel.SendToastSpampack(NavigationContext.QueryString[PushNotificationsViewModel.SPAMPACK_TAG]);
            //    }
            //}
        }

	    private void CategoryBooks_OnLoadMore(object sender, EventArgs e)
	    {
            ViewModel.LoadMoreCalled.Execute(null);
        }

	    private void CategoryBooks_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
           if(CategoryBooks.SelectedItem != null) ViewModel.BookSelected.Execute(CategoryBooks.SelectedItem);  
        }

	    private Task CategoryBooks_OnMoreDataRequested(object sender, EventArgs e)
	    {
            ViewModel.LoadMoreCalled.Execute(null);
            return Task.Run(() => { });
	    }
	}

	public class BooksByCategoryFitting : ViewModelPage<BooksByCategoryViewModel>
	{
	}
}