using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using BookParser;
using LitRes;
using LitRes.Views;
using LitResReadW10.Helpers;

namespace LitResReadW10.Views
{
    public sealed partial class BookContent
    {
        readonly Reader _readerPage = Reader.Instance;

        private ObservableCollection<Chapters> _bookChapters;

        public BookContent()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Analytics.Instance.sendMessage(Analytics.ViewTOC);
            TockListView.SelectionChanged -= TockListViewOnSelectionChanged;
            TockListView.SelectionChanged += TockListViewOnSelectionChanged;
            _bookChapters = new ObservableCollection<Chapters>();
            
            foreach (var item in AppSettings.Default.Chapters.Select(chapter => new Chapters
            {
                Title = chapter.Title,
                Page = (int) Math.Ceiling((double) (chapter.TokenID + 1)/AppSettings.WordsPerPage),
                Level = chapter.Level,
                TokenId = chapter.TokenID
            }))
            {
                _bookChapters.Add(item);
            }
            TockListView.ItemsSource = null;
            TockListView.ItemsSource = _bookChapters;
            TockListView.SelectedIndex = -1;
            TockListView.SelectedItem = null;
        }

        

        private void TockListViewOnSelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            if (_readerPage == null) return;
            var list = (ListView)sender;
            var index = list.SelectedIndex;
            if (index < 0) return;
            var chapter = list.SelectedItem as Chapters;
            if (chapter == null) return;
            AppSettings.Default.CurrentTokenOffset = chapter.TokenId;
            AppSettings.Default.ToChapter = true;
            if (SystemInfoHelper.IsDesktop())
                _readerPage.GoToChapter();
            else if (Frame.CanGoBack) Frame.GoBack();
        }
    }

    public class Chapters
    {
        public string Title { get; set; }
        public int Page { get; set; }
        public int TokenId { get; set; }
        public int Level { get; set; }

        public Chapters(string title, int page, int token, int level)
        {
            Title = title;
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
