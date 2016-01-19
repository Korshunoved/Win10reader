using System.Globalization;
using System.IO;
using System.Text;
using Digillect.Collections;
using LitRes.Models;

namespace LitRes.ViewModels
{
	public static class BookBuilder
	{
        //public static void BuildBookFromDocument(Book book, FictionBook.Document document)
        //{
        //    if (book.Description != null || document == null) return;         
           
        //    book.Id = 0;
        //    var charArr = MD5.GetMd5String(document.Title).ToCharArray();
        //    foreach (var ch in charArr) book.Id -= ch;
        //    book.HasTrial = "0";
        //    book.IsLocal = true;
        //    book.InappName = string.Empty;
        //    book.InappPrice = 0;
        //    book.IsMyBook = true;
        //    book.Chars = "0";
        //    book.Rating = "0";
        //    book.ReadedPercent = 0;
        //    book.Recenses = "0";
        //    if (document.Description != null)
        //    {
        //        var docPublishInfo = document.Description.PublishInfo;
        //        book.Copyright = docPublishInfo != null ? docPublishInfo.Publisher.Text : null;

        //        book.Description = new Book.TextDescription
        //        {
        //            Hidden = new Book.Hidden
        //            {
        //                DocumentInfo = new Book.DocumentInfo {Id = document.Id ?? book.Id.ToString()}
        //            }
        //        };
        //        if (docPublishInfo == null)
        //        {
        //            book.Description.Hidden.PublishInfo = new Book.PublishInfo
        //            {
        //                BookName = null,
        //                Isbn = null,
        //                City = null,
        //                Publisher = null,
        //                Year = 0,
        //                Sequence = null
        //            };
        //        }
        //        else
        //        {
        //            book.Description.Hidden.PublishInfo = new Book.PublishInfo
        //            {
        //                BookName = docPublishInfo.BookName != null ? docPublishInfo.BookName.Text : null,
        //                Isbn = docPublishInfo.ISBN != null ? docPublishInfo.ISBN.Text : null,
        //                City = docPublishInfo.City != null ? docPublishInfo.City.Text : null,
        //                Publisher = docPublishInfo.Publisher != null ? docPublishInfo.Publisher.Text : null,
        //                Year = docPublishInfo.Year,
        //                Sequence = null
        //            };
        //        }

        //        var dTitleInfo = document.Description.TitleInfo;
        //        if (dTitleInfo != null)
        //        {
        //            book.Description.Hidden.TitleInfo = new Book.TitleInfo
        //            {
        //                Language = dTitleInfo.Language,
        //                SourceLanguage = dTitleInfo.SourceLanguage,
        //                Sequence = new Book.SequenceInfo(),
        //                Coverpage = new Book.TitleInfo.CoverpageInfo
        //                {
        //                    Image = new Book.TitleInfo.CoverpageInfo.ImageInfo
        //                    {
        //                        Href =
        //                            (dTitleInfo.Coverpage != null && dTitleInfo.Coverpage.Images != null &&
        //                             dTitleInfo.Coverpage.Images.Count > 0)
        //                                ? dTitleInfo.Coverpage.Images[0].Reference
        //                                : string.Empty
        //                    }
        //                }
        //            };
        //            if (dTitleInfo.Authors != null)
        //            {
        //                var authorsInfo = new Book.TitleInfo.AuthorInfo[dTitleInfo.Authors.Count];
        //                int i = 0;
        //                foreach (var author in dTitleInfo.Authors)
        //                {
        //                    var bookAuthorInfo = new Book.TitleInfo.AuthorInfo
        //                    {
        //                        Id = string.Empty,
        //                        FirstName = author.FirstName != null ? author.FirstName.Text : string.Empty,
        //                        LastName = author.LastName != null ? author.LastName.Text : string.Empty,
        //                        MiddleName = author.MiddleName != null ? author.MiddleName.Text : string.Empty
        //                    };
        //                    authorsInfo[i] = bookAuthorInfo;
        //                    ++i;
        //                }
        //                book.Description.Hidden.TitleInfo.Author = authorsInfo;
        //            }
        //            else
        //            {
        //                book.Description.Hidden.TitleInfo.Author = new Book.TitleInfo.AuthorInfo[0];
        //            }

        //            if (dTitleInfo.Annotation != null && dTitleInfo.Annotation.Children != null)
        //            {
        //                book.Description.Hidden.TitleInfo.Annotation = new Book.Annotation();
        //                var sB = new StringBuilder();
        //                foreach (var child in dTitleInfo.Annotation.Children) sB.Append(child.ToString());
        //                book.Description.Hidden.TitleInfo.Annotation.Text = sB.ToString();
        //            }

        //            if (dTitleInfo.Translators != null)
        //            {
        //                var transl = new XCollection<Book.TitleInfo.AuthorInfo>();
        //                if (dTitleInfo.Translators != null)
        //                {
        //                    foreach (var translator in dTitleInfo.Translators)
        //                    {
        //                        var tr = new Book.TitleInfo.AuthorInfo
        //                        {
        //                            Id = string.Empty,
        //                            FirstName = translator.FirstName != null ? translator.FirstName.Text : string.Empty,
        //                            LastName = translator.LastName != null ? translator.LastName.Text : string.Empty,
        //                            MiddleName =
        //                                translator.MiddleName != null ? translator.MiddleName.Text : string.Empty
        //                        };
        //                        transl.Add(tr);
        //                    }
        //                    book.Description.Hidden.TitleInfo.Translators = transl;
        //                }
        //            }

        //            if (dTitleInfo.Sequences != null && dTitleInfo.Sequences.Count > 0)
        //            {
        //                book.Description.Hidden.TitleInfo.Sequence.Name = dTitleInfo.Sequences[0].Name;
        //                book.Description.Hidden.TitleInfo.Sequence.Number = dTitleInfo.Sequences[0].Number;
        //            }

        //            if (dTitleInfo.Genres != null && dTitleInfo.Genres.Count > 0)
        //            {
        //                book.Description.Hidden.TitleInfo.Genres = new string[dTitleInfo.Genres.Count];
        //                for (int i = 0; i < dTitleInfo.Genres.Count; ++i)
        //                {
        //                    book.Description.Hidden.TitleInfo.Genres[i] = dTitleInfo.Genres[i].Name;
        //                }
        //            }
        //            book.Description.Hidden.TitleInfo.BookTitle = dTitleInfo.Title != null
        //                ? dTitleInfo.Title.Text
        //                : string.Empty;
        //        }
        //        else
        //        {
        //            book.Description.Hidden.TitleInfo = null;
        //        }
        //    }
        //    foreach (var bin in document.Binaries)
        //    {
        //        if (bin.ContentType.Equals("image/jpeg") && bin.Id.Contains("cover"))
        //        {
        //            var coverPath = string.Format("cover{0}.jpg", book.Id.ToString(CultureInfo.InvariantCulture));
        //            coverPath = Path.Combine("MyBooks", coverPath);
        //            book.Cover = coverPath;
        //            book.CoverPreview = coverPath;
        //            break;
        //        }
        //    }
        //}
	}
}
