﻿using System.Threading;
using System.Threading.Tasks;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.Services
{
    public interface IBookProvider
    {
        //Task<string> GetFullBook(Book book, CancellationToken token);
        Task<FictionBook.Document> GetFullBook(Book book, CancellationToken token);
        //Task<FictionBook.Document> GetLocalFullBook(string filetoken, Book book, CancellationToken token);
        Task<FictionBook.Document> GetTrialBook(Book book, CancellationToken token);
        Task<XCollection<Book>> GetExistBooks(CancellationToken token);
        Task UpdateExistBook(Book book, CancellationToken token);
        bool FullBookExistsInLocalStorage(int bookId);
        bool TrialBookExistsInLocalStorage(int bookId);
        Task RemoveFullBookInLocalStorage(Book book);
        void RemoveTrialBookInLocalStorage(Book book);
        Task ClearLibrariesBooks(CancellationToken token);
        Task ClearNotLoadedBooks(CancellationToken token);
    }
}
