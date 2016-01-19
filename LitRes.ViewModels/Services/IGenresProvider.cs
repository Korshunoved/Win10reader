using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
	public interface IGenresProvider
	{
		Task<Genre> GetGenres(CancellationToken cancellationToken);
		Task<Genre> GetGenreById(int genreId, CancellationToken cancellationToken);

		/// <summary>
		/// Get genre by index
		/// </summary>
		/// <param name="index">Index is a calculated string, created as splitted indices, separated by ":"</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task<Genre> GetGenreByIndex(string index, CancellationToken cancellationToken);

		/// <summary>
		/// Get index by genre
		/// </summary>
		/// <param name="genre">Genre, the index of which is necessary to find</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task<string> GetIndexByGenre(Genre genre, CancellationToken cancellationToken);
		Task<XCollection<Genre>> GetGenresByTokens(string[] tokens, CancellationToken cancellationToken);
	}
}
