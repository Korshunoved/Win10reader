using System;
using Autofac;
using Digillect.Mvvm.Services;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using Microsoft.Phone.Shell;

namespace LitRes.Views
{
	[View( "Genre" )]
	public partial class Genre : GenreFitting
	{
		private bool _liked;

		#region Constructors/Disposer
		public Genre()
		{
			InitializeComponent();
		}
		#endregion

		//private void Genre_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		//{
		//	Scope.Resolve<INavigationService>().Navigate( "GenreBooks" );
		//}

		#region View Events
		private void appbarLike_Click(object sender, EventArgs e)
		{
			ApplicationBarIconButton btn = (ApplicationBarIconButton) ApplicationBar.Buttons[0];

			if ( _liked )
			{
				btn.Text = "следить за новинками";
				btn.IconUri = new Uri("/assets/appbar/like.png", UriKind.Relative);
			}
			else
			{
				btn.Text = "не следить за новинками";
				btn.IconUri = new Uri("/assets/appbar/unlike.png", UriKind.Relative);
			}

			_liked = !_liked;
		}
		#endregion
	}

	public class GenreFitting : EntityPage<int, LitRes.Models.Genre, GenreViewModel>
	{
	}
}