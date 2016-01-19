using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Digillect.Mvvm.UI;

using LitRes;
using LitRes.Models;
using LitRes.ViewModels;

namespace LitRes.Views
{
    [View("NokiaBooksByCategory")]
	[ViewParameter("category", typeof(int))]
    [ViewParameter("id", typeof(int))]
	public partial class NokiaBooksByCategory : NokiaBooksByCategoryFitting
	{
		#region Constructors/Disposer
        public NokiaBooksByCategory()
		{
            Debug.WriteLine("NokiaBooksByCategory");
			InitializeComponent();
		}
		#endregion

		#region CreateDataSession
		protected override Digillect.Mvvm.Session CreateDataSession( DataLoadReason reason )
		{
            if (ViewParameters.Get<int>("id") != 0) ViewModel.BooksViewModelType = ViewParameters.Get<int>("id");
			else ViewModel.BooksViewModelType = ViewParameters.Get<int>( "category" );

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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("NavigatedFrom"))
            {
                if (NavigationContext.QueryString["NavigatedFrom"].Equals("toast"))
                {                   
                    if (NavigationContext.QueryString.ContainsKey(PushNotificationsViewModel.SPAMPACK_TAG))
                        PushNotificationsViewModel.SendToastSpampack(NavigationContext.QueryString[PushNotificationsViewModel.SPAMPACK_TAG]);
                }
            }
        }
	}

	public class NokiaBooksByCategoryFitting : ViewModelPage<BooksByCategoryViewModel>
	{
	}
}