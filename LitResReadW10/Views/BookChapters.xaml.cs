
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using BookParser;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using LitResReadW10;
using LitResReadW10.Controls;
using LitResReadW10.Helpers;

namespace LitRes.Views
{
	[View("BookChapters")]
	public partial class BookChapters : BookChaptersFitting
    {
        Reader readerPage = Reader.Instance;

        #region Constructors/Disposer
        public BookChapters()
		{
			InitializeComponent();
            Loaded += BookChapters_Loaded;
		}
        #endregion

        void BookChapters_Loaded(object sender, RoutedEventArgs e)
        {
            Analytics.Instance.sendMessage(Analytics.ViewTOC);
            var appChapters = AppSettings.Default.Chapters;
            List<Chapters> Chapters =
                appChapters.Select(
                    chapterModel =>
                        new Chapters
                        {
                            Title = chapterModel.Title,
                            Page = (int) Math.Ceiling((double) (chapterModel.TokenID + 1)/AppSettings.WORDS_PER_PAGE),
                            Level = chapterModel.Level,
                            TokenId = chapterModel.TokenID
                        }).ToList();
            TockListView.ItemsSource = Chapters;
            TockListView.SelectionChanged += TockListViewOnSelectionChanged;
        }

        private void TockListViewOnSelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            //if (readerPage == null) return;
            //var list = (ListView)sender;
            //var index = list.SelectedIndex;
            //if (index <= 0) return;
            //var chapter = list.SelectedItem as Chapters;
            //if (chapter == null) return;
            //AppSettings.Default.CurrentTokenOffset = chapter.TokenId;
            //AppSettings.Default.ToChapter = true;            
            //if (SystemInfoHelper.IsDesktop())
            //    readerPage.GoToChapter();
        }

        private void TockListView_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            LocalBroadcastReciver.Instance.OnPropertyChanging(TockListView.SelectedItem, new PropertyChangingEventArgs("TocTapped"));
            if (readerPage == null) return;
            var list = (ListView)sender;
            var index = list.SelectedIndex;
            if (index <= 0) return;
            var chapter = list.SelectedItem as Chapters;
            if (chapter == null) return;
            AppSettings.Default.CurrentTokenOffset = chapter.TokenId;
            AppSettings.Default.ToChapter = true;
            if (SystemInfoHelper.IsDesktop())
                readerPage.GoToChapter();
            if (!SystemInfoHelper.IsDesktop() && Frame.CanGoBack) Frame.GoBack();
        }
    }

    public class BookChaptersFitting : EntityPage<BookChaptersViewModel.ChapterRootNode, BookChaptersViewModel>
	{
	}

    public class Chapters
    {
        public string Title { get; set; }
        public int Page { get; set; }
        public int TokenId { get; set; }
        public int Level { get; set; }

        public Chapters(string title, int page, int token, int level)
        {
            Title = title.Trim();
            Page = page + 1;
            TokenId = token;
            Level = level;
        }

        public Chapters()
        {
            Title = "";
            Page = 0;
            Level = 0;
            TokenId = 0;
        }
    }
}