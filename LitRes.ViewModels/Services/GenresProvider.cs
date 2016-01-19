using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using LitRes.Models;
using LitRes.Services.Connectivity;

namespace LitRes.Services
{
	internal class GenresProvider : IGenresProvider
	{
		const string CacheItemName = "genres";
		const int CacheAgeInDays = 20;

		private readonly ICatalitClient _client;
		private readonly IDataCacheService _dataCacheService;

		private Genre _genres;

		#region Constructors/Disposer
		public GenresProvider( ICatalitClient client, IDataCacheService dataCacheService )
		{
			_client = client;
			_dataCacheService = dataCacheService;
		}
		#endregion

		public async Task<Genre> GetGenres( CancellationToken cancellationToken )
		{
			if( _genres == null )
			{
				DateTime cacheModificationDate = await _dataCacheService.GetItemModificationDate( CacheItemName );

				if( (DateTime.Now - cacheModificationDate) < TimeSpan.FromDays( CacheAgeInDays ) )
				{
					try
					{
						_genres = _dataCacheService.GetItem<Genre>( CacheItemName );
					}
					catch (Exception ex)
					{
					    Debug.WriteLine(ex.Message);
					}
				}

				if( _genres == null )
				{
					_genres = await _client.GetGenres( cancellationToken );

					_dataCacheService.PutItem<Genre>( _genres, CacheItemName, cancellationToken );
				}
			}

			return _genres;
		}

		public async Task<Genre> GetGenreById( int genreId, CancellationToken cancellationToken )
		{
			var genres = await GetGenres(cancellationToken);
			if (genres != null)
			{
				return GetGenreInCollectionById(genres.Children, genreId);
			}
			return null;
		}

		private Genre GetGenreInCollectionById(XCollection<Genre> genres, int genreId)
		{
			Genre found = null;

			if (genres != null)
			{
				found = genres.FirstOrDefault(x => x.Id == genreId);

				if (found == null)
				{
					foreach (var genre in genres)
					{
						found = GetGenreInCollectionById(genre.Children, genreId);

						if (found != null)
						{
							break;
						}
					}
				}
			}
			return found;
		}

		public async Task<Genre> GetGenreByIndex( string index, CancellationToken cancellationToken )
		{
			var genres = await GetGenres(cancellationToken);
			if (genres != null && !string.IsNullOrEmpty(index))
			{
				List<string> indexes = new List<string>(index.Split(':'));
				return GetGenreInCollectionByIndex(genres.Children, indexes);
			}
			return null;
		}

		private Genre GetGenreInCollectionByIndex(XCollection<Genre> genres, List<string> indexes)
		{
			Genre res = null;
			if (genres != null)
			{
				if (indexes.Count > 0)
				{
					int idx = Convert.ToInt32(indexes[0]);
					if (idx < genres.Count)
					{
						res = genres[idx];
					}
				}
				if (indexes.Count > 1 && res != null)
				{
					indexes.RemoveAt(0);
					res = GetGenreInCollectionByIndex(res.Children, indexes);
				}
			}
			return res;
		}

		public async Task<string> GetIndexByGenre( Genre genre, CancellationToken cancellationToken )
		{
			var genres = await GetGenres(cancellationToken);
			if (genres != null && genre != null)
			{
				return GetIndexByGenreInCollection(genres.Children, genre);
			}
			return null;
		}

		private string GetIndexByGenreInCollection(XCollection<Genre> genres, Genre genre)
		{
			string res = string.Empty;
			if (genres != null)
			{
				if (genres.Contains(genre))
				{
					int index = genres.IndexOf(genre);
					return index.ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					for (int i = 0; i < genres.Count; i++)
					{
						string idx = GetIndexByGenreInCollection(genres[i].Children, genre);
						if (!string.IsNullOrEmpty(idx))
						{
							res = i.ToString(CultureInfo.InvariantCulture) + ":" + idx;
							break;
						}
					}
				}
			}
			return res;
		}

		public async Task<XCollection<Genre>> GetGenresByTokens(string[] tokens, CancellationToken cancellationToken)
		{
			var genres = await GetGenres(cancellationToken);
			if (genres != null && tokens != null && tokens.Length > 0)
			{
				List<string> tokensList = new List<string>(tokens);
				return GetGenresInCollectionByTokens(genres.Children, tokensList);
			}
			return null;
		}

		private XCollection<Genre> GetGenresInCollectionByTokens(XCollection<Genre> genres, List<string> tokens)
		{
			XCollection<Genre> res = new XCollection<Genre>();
			if (genres != null)
			{
				foreach (var genre in genres)
				{
					if(tokens.Contains( genre.Token ) && res.FirstOrDefault( x => x.Id == genre.Id ) == null)
					{
						res.Add( genre );
					}
					var subcollection = GetGenresInCollectionByTokens(genre.Children, tokens);
					foreach (var subgenre in subcollection)
					{
						if(res.FirstOrDefault( x => x.Id == subgenre.Id ) == null)
						{
							res.Add(subgenre);
						}
					}
				}
			}
			return res;
		}
	}
}
