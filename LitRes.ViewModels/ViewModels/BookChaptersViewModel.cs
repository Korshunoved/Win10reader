using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Digillect;
using Digillect.Collections;
using Digillect.Mvvm;
using Newtonsoft.Json.Linq;

namespace LitRes.ViewModels
{
	public class BookChaptersViewModel : EntityViewModel<BookChaptersViewModel.ChapterRootNode>
	{
		#region BookChapterNode
		public class BookChapterNode : XObject
		{
			public string Title { get; set; }
			public string Text { get; set; }
		}
		#endregion

		#region Public Properties
		public XCollection<Chapter> Chapters { get; private set; }

		public RelayCommand<Chapter> ReadByChapter { get; private set; }
		#endregion

		#region Constructors/Disposer
		public BookChaptersViewModel()
		{
			Chapters = new XCollection<Chapter>();
			ReadByChapter = new RelayCommand<Chapter>( MoveToChapter );
		}

		#endregion

		#region LoadEntity
		protected override async Task LoadEntity( Session session )
		{
		    var tocJson = session.Parameters.GetValue<string>("TocJson");

			Entity = GetTocFromJSON(tocJson);
            Chapters.AddRange(Entity.Chapters[0].Chapters);
		}
		#endregion

		#region MoveToChapter

		private void MoveToChapter(Chapter chapter)
		{
           OnPropertyChanged();
		}

        #endregion
        #region LoadChapters
        public ChapterRootNode GetTocFromJSON(string json)
        {
            try
            {
                var root = JArray.Parse(json);
                if (root.Count > 0)
                {
                    var chapterRoot = new ChapterRootNode();
                    foreach (var rootChapter in root)
                    {
                        if (rootChapter["t"] == null) continue;
                        var chapterTitleNode = new ChapterTitleNode
                        {
                            ChapterInfo = new Chapter
                            {
                                t = rootChapter["t"]?.ToString(),
                                e = rootChapter["e"]?.ToString(),
                                s = rootChapter["s"]?.ToString(),
                            }
                        };
                        var localChapters = rootChapter["c"];
                        if (localChapters == null) continue;
                        for (var j = 0; j < localChapters.Count(); ++j)
                        {
                            if (localChapters[j]["t"] != null)
                            {
                                chapterTitleNode.Chapters.Add(new Chapter
                                {
                                    t = localChapters[j]["t"]?.ToString(),
                                    e = localChapters[j]["e"]?.ToString(),
                                    s = localChapters[j]["s"]?.ToString()
                                });
                            }
                        }
                        chapterRoot.Chapters.Add(chapterTitleNode);
                    }
                    return chapterRoot;
                }
                Debug.WriteLine(root.ToString());

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return null;
        }

        public class ChapterRootNode : XObject
        {
            public ChapterRootNode()
            {
                Chapters = new XCollection<ChapterTitleNode>();
            }

            public XCollection<ChapterTitleNode> Chapters { get; private set; }
        }

        public class ChapterTitleNode : XObject
        {
            public ChapterTitleNode()
            {
                Chapters = new XCollection<Chapter>();
            }
            public Chapter ChapterInfo { get; set; }
            public XCollection<Chapter> Chapters { get; private set; }
        }

        public class Chapter : XObject
        {
            public string s { get; set; }
            public string e { get; set; }
            public string t { get; set; }
        }
        #endregion
    }
}
