using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LitRes.Models;
using LitRes.ViewModels;

namespace LitRes.Services
{
	internal class SettingsService : ISettingsService
	{
		private const string CacheItemName = "settings";

		private readonly IDataCacheService _dataCacheService;
		private ReaderSettings _settings;

		#region Constructors/Disposer
		/// <summary>
		/// Initializes a new instance of the SettingsService class.
		/// </summary>
		/// <param name="dataCacheService"></param>
		public SettingsService( IDataCacheService dataCacheService )
		{
			_dataCacheService = dataCacheService;
		}
		#endregion

        public ReaderSettingsViewModel.DeffaultSettingsType DeffaultSettings { get; set; }

		public Task<ReaderSettings> GetSettings()
		{
			if( _settings == null )
			{
				_settings = _dataCacheService.GetItem<ReaderSettings>( CacheItemName );

				if( _settings == null )
				{
                    if (DeffaultSettings == ReaderSettingsViewModel.DeffaultSettingsType.DeffaultSettingsTypeHD) _settings = new ReaderSettings {SystemTiles = false, Margin = 2, Brightness = 1, Theme = 2, Font = 1, FontSize = 0, CharacterSpacing = 1, Hyphenate = true, Autorotate = true, LastUpdate = DateTime.Now, AnimationMoveToPage = true };
                    else _settings = new ReaderSettings { SystemTiles = false, Margin = 2, Brightness = 0, Theme = 2, Font = 1, FontSize = 2, CharacterSpacing = 0, Hyphenate = true, Autorotate = true, LastUpdate = DateTime.Now, AnimationMoveToPage = true };
				}
			}

			return Task.FromResult( _settings );
		}

		public void SetSettings( ReaderSettings settings )
		{
			if( settings != null )
			{
				if( _settings == null )
				{
					_settings = (ReaderSettings) settings.Clone( false );
				}
				else
				{
					_settings.Update( settings );
				}

				_dataCacheService.PutItem( _settings, CacheItemName, CancellationToken.None );
			}
		}
	}
}
