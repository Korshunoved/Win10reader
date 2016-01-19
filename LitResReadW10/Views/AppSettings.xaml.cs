using System;
using System.Windows;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace LitRes.Views
{
    [View("AppSettings")]
    [ViewParameter("PDFBook", typeof(bool))]
	public partial class AppSettings : AppSettingsFitting
	{
		#region Constructors/Disposer
		public AppSettings()
		{
			InitializeComponent();

			Loaded += Settings_Loaded;
		}
		#endregion

	    protected override Session CreateDataSession(DataLoadReason reason)
	    {
            ViewModel.IsDefaultSettings = !ViewParameters.Get<bool>("PDFBook");        
            return base.CreateDataSession(reason);
	    }

		void Settings_Loaded( object sender, RoutedEventArgs e )
		{
		}

		private void tsSysTile_Unchecked( object sender, System.Windows.RoutedEventArgs e )
		{
			(sender as ToggleSwitch).Content = "Выключен";
		    SetSystemTileEnable(false);

		}

        private void tsSysTile_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			(sender as ToggleSwitch).Content = "Включен";
            SetSystemTileEnable(true);
		}

	    private void SetSystemTileEnable(bool enable)
	    {
            foreach (ShellTile TileToSchedule in ShellTile.ActiveTiles)
            {
                if (TileToSchedule.NavigationUri.Equals(new Uri("/", UriKind.RelativeOrAbsolute)))
                {
                    ShellTileData newTile = null;
                    if (enable)
                    {
                        newTile = new FlipTileData
                        {
                            BackgroundImage = new Uri(@"/Assets/Tiles/FlipCycleTileMediumOp.png", UriKind.Relative),
                            WideBackgroundImage = new Uri(@"/Assets/Tiles/FlipCycleTileLargeOp.png", UriKind.Relative),
                            SmallBackgroundImage = new Uri(@"/Assets/Tiles/FlipCycleTileSmallOp.png", UriKind.Relative),
                            Title = "Читай!"
                        };
                    }
                    else
                    {
                        newTile = new FlipTileData
                        {
                            BackgroundImage = new Uri(@"/Assets/Tiles/FlipCycleTileMedium.png", UriKind.Relative),
                            WideBackgroundImage = new Uri(@"/Assets/Tiles/FlipCycleTileLarge.png", UriKind.Relative),
                            SmallBackgroundImage = new Uri(@"/Assets/Tiles/FlipCycleTileSmall.png", UriKind.Relative),
                        };
                    }
                    TileToSchedule.Update(newTile);
                    Console.WriteLine("Tile updated");
                    break;
                }
            }
	    }

		protected override async void OnBackKeyPress( System.ComponentModel.CancelEventArgs e )
		{
			await ViewModel.Save();
			base.OnBackKeyPress( e );
		}
	}

	public class AppSettingsFitting : ViewModelPage<ReaderSettingsViewModel>
	{
	}
}