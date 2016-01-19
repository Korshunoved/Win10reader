﻿using System;
using System.Threading.Tasks;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class ReaderSettingsViewModel : ViewModel
	{
		private const string LoadSettingsPart = "Load";
		private const string SaveSettingsPart = "Save";

		private readonly ISettingsService _settingsService;

		private readonly ReaderSettings _settings;

		private bool _autorotate;
		private bool _fitWidth;
        private bool _hyphenate;
        private bool _animate;
		private int _theme;
		private int _font;
		private int _fontSize;
		private int _margins;
		private int _interlineage;
        private bool _systemTile;

        public enum DeffaultSettingsType
        {
            DeffaultSettingsTypeNormal = 0,
            DeffaultSettingsTypeHD,
        }


		#region Constructors/Disposer
		public ReaderSettingsViewModel( ISettingsService settingsService )
		{
			_settingsService = settingsService;


            RegisterAction(LoadSettingsPart).AddPart(session => LoadSettings(session), session => true);
            RegisterAction(SaveSettingsPart).AddPart(session => SaveSettings(session), session => true);
            //RegisterPart( LoadSettingsPart, ( session, part ) => LoadSettings( session ), ( session, s ) => true, true );
            //RegisterPart( SaveSettingsPart, ( session, part ) => SaveSettings( session ) , ( session, s ) => true, false );

            _settings = new ReaderSettings();

		    Task.Run(async () => await Load(new Session(LoadSettingsPart)));
		}
		#endregion

		#region Properties
		public ReaderSettings Settings
		{
			get { return _settings; }
		}

        public bool IsDefaultSettings { get; set; }

        public bool SystemTile
        {
            get { return _systemTile; }
            set { SetProperty(ref _systemTile, value, "SystemTile"); }
        }

		public bool Autorotate
		{
			get { return _autorotate; }
			set { SetProperty( ref _autorotate, value, "Autorotate" ); }
		}

		public bool FitWidth
		{
			get { return _fitWidth; }
			set { SetProperty( ref _fitWidth, value, "FitWidth" ); }
		}

		public bool Hyphenate
		{
			get { return _hyphenate; }
			set { SetProperty(ref _hyphenate, value, "Hyphenate"); }
		}

        public bool AnimationMoveToPage
		{
            get { return _animate; }
            set { SetProperty(ref _animate, value, "AnimationMoveToPage"); }
		}

		public int Theme
		{
			get { return _theme; }
			set { SetProperty( ref _theme, value, "Theme" ); }
		}

		public int Font
		{
			get { return _font; }
			set { SetProperty( ref _font, value, "Font" ); }
		}

		public int FontSize
		{
			get { return _fontSize; }
			set { SetProperty( ref _fontSize, value, "FontSize" ); }
		}

		public int Margins
		{
			get { return _margins; }
			set { SetProperty( ref _margins, value, "Margins" ); }
		}

		public int Interlineage
		{
			get { return _interlineage; }
			set { SetProperty( ref _interlineage, value, "Interlineage" ); }
		}

	    #endregion

		#region Save
		public async Task Save()
		{
			await Load( new Session( SaveSettingsPart ) );
		}
		#endregion

		#region LoadSettings
		private async Task LoadSettings( Session session )
		{
			var settings = await _settingsService.GetSettings();

			if( settings != null )
			{
				_settings.Update( settings );

				Autorotate = settings.Autorotate;
				FitWidth = settings.FitWidth;
				Hyphenate = settings.Hyphenate;
                AnimationMoveToPage = Settings.AnimationMoveToPage;
				Theme = settings.Theme;
				Font = settings.Font;
				FontSize = settings.FontSize;
				Margins = settings.Margin;
				Interlineage = settings.CharacterSpacing;
			    SystemTile = settings.SystemTiles;
			}
		}
		#endregion

		#region SaveSettings
		private async Task SaveSettings( Session session )
		{
			var settings = await _settingsService.GetSettings();

			_settings.Autorotate = _autorotate;
			_settings.FitWidth = _fitWidth;
			_settings.Hyphenate = _hyphenate;
            _settings.AnimationMoveToPage = _animate;
			_settings.Theme = _theme;
			_settings.Font = _font;
			_settings.FontSize = _fontSize;
			_settings.Margin = _margins;
			_settings.CharacterSpacing = _interlineage;
		    _settings.SystemTiles = SystemTile;
			if( !settings.Equals( _settings ) )
			{
				_settings.LastUpdate = DateTime.Now;
				_settingsService.SetSettings( _settings );
			}
		}
		#endregion
	}
}
