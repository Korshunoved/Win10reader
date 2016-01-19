using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Services;

namespace LitRes.ViewModels
{
	public class GenreViewModel : EntityViewModel<int, Genre>
	{
		private readonly IGenresProvider _genresProvider;
		private readonly INavigationService _navigationService;

		#region Constructors/Disposer
		public GenreViewModel(IGenresProvider genresProvider, INavigationService navigationService)
		{
			_genresProvider = genresProvider;
			_navigationService = navigationService;

			GenreSelected = new RelayCommand<Genre>(genre => _navigationService.Navigate("GenreBooks", Parameters.From("id", genre.Id)), genre => genre != null);
		}
		#endregion

		#region Public Properties
		public RelayCommand<Genre> GenreSelected { get; private set; }
		#endregion

		#region LoadEntity
		protected override async Task LoadEntity(EntitySession<int> session)
		{
			Entity = await _genresProvider.GetGenreByIndex(session.Id.ToString(CultureInfo.InvariantCulture), session.Token);
		}
		#endregion
	}
}
