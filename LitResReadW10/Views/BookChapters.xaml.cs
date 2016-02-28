
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

        void BookChapters_Loaded(object sender, RoutedEventArgs e)
        {
            Analytics.Instance.sendMessage(Analytics.ViewTOC);
            var appChapters = AppSettings.Default.Chapters;
            var book = AppSettings.Default.CurrentBook;
            List<Chapters> Chapters = appChapters.Select(chapterModel => new Chapters {Title = chapterModel.Title, Page = (int) Math.Ceiling((double) (chapterModel.TokenID + 1)/AppSettings.WORDS_PER_PAGE)}).ToList();
            TockListView.ItemsSource = Chapters;
            TockListView.SelectionChanged += TockListViewOnSelectionChanged;
        }

	    private void TockListViewOnSelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
	    {
	        if (readerPage == null) return;
	        var list = (ListView) sender;
	        var index = list.SelectedIndex;
	        if (index <= 0) return;
	        var chapter = list.SelectedItem as Chapters;
	        if (chapter == null) return;
            readerPage.CurrentPage = chapter.Page;
	        readerPage.GoToChapter();
	    }

	    #endregion

	    private void TockListView_OnTapped(object sender, TappedRoutedEventArgs e)
	    {
            LocalBroadcastReciver.Instance.OnPropertyChanging(TockListView.SelectedItem, new PropertyChangingEventArgs("TocTapped"));
            if(!SystemInfoHelper.IsDesktop() && Frame.CanGoBack) Frame.GoBack();
        }
    }

    public class BookChaptersFitting : EntityPage<BookChaptersViewModel.ChapterRootNode, BookChaptersViewModel>
	{
	}

    public class Chapters
    {
        public string Title { get; set; }
        public int Page { get; set; }

        public Chapters(string title, int page)
        {
            Title = title;
            Page = page + 1;
        }

        public Chapters()
        {
            Title = "";
            Page = 0;
        }
    }
}