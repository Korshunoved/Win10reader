
using System;
using System.Diagnostics;
using System.Linq;
using Digillect;
using Digillect.Collections;

namespace LitRes.Models.JsonModels
{

    public class Rootobject
    {
        public object bestResult { get; set; }
        public Tracker tracker { get; set; }
        public Art[] arts { get; set; }
        public Author[] authors { get; set; }
        public Genre[] genres { get; set; }
        public string search_string { get; set; }
        public Series[] series { get; set; }
        public Collection[] collections { get; set; }
        public Tag[] tags { get; set; }

        public XCollection<Person> GetPersons()
        {
            var persons = new XCollection<Person>();
            if (authors != null)
            {
                foreach (var author in authors)
                {
                    var person = author.ToPerson();
                    persons.Add(person);
                }
            }
            return persons;
        }

        public XCollection<Book> GetBooks()
        {
            var artsCollection = new XCollection<Book>();
            if (arts != null)
            {
                foreach (var art in arts)
                {
                    var book = art.ToBook();
                    artsCollection.Add(book);
                }
            }
            return artsCollection;
        }

        public XCollection<Book.SequenceInfo> GetSequences()
        {
            var sequences = new XCollection<Book.SequenceInfo>();
            if (series != null)
            {
                foreach (var seria in series)
                {
                    try
                    {
                        var imageUrl = string.Format("{0}{1}", "http://wp8-ebook.litres.ru", seria.img);

                        sequences.Add(new Book.SequenceInfo
                        {
                            Id = int.Parse(seria.id),
                            Number = int.Parse(seria.total_arts),
                            Name = seria.name,
                            ImgUrl = imageUrl
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            return sequences;
        }

        public XCollection<Book.CollectionsInfo> GetCollection()
        {
            var sequences = new XCollection<Book.CollectionsInfo>();
            if (collections != null)
            {
                foreach (var collection in collections)
                {
                    try
                    {
                        var imageUrl = string.Format("{0}{1}", "http://wp8-ebook.litres.ru", collection.img);

                        sequences.Add(new Book.CollectionsInfo
                        {
                            Id = int.Parse(collection.id),
                            Number = int.Parse(collection.arts_count),
                            Name = collection.name,
                            ImgUrl = imageUrl
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            return sequences;
        }

        public string FoundResults()
        {
            int res = 0;

            if(arts != null) res += arts.Length;
            if (authors != null) res += authors.Length;
            if(genres != null)if (arts != null) res += genres.Length;
            if (series != null) res += series.Length;
            //if (collections != null) res += collections.Length;
            if (tags != null) res += tags.Length;

            return Convert.ToString(res);   
        }
    }

    public class Tracker
    {
        public string s_sid { get; set; }
        public int by_rmd { get; set; }
        public int user { get; set; }
    }

    public class Art
    {
        public string mark { get; set; }
        public string id { get; set; }
        public string marks { get; set; }
        public string atype { get; set; }
        public string name { get; set; }
        public string img_h { get; set; }
        public string img { get; set; }
        public string img_w { get; set; }
        public string author { get; set; }
        public string url { get; set; }
        public string freebie { get; set; }
        public double base_price { get; set; }

        public Book ToBook()
        {
            var authorsInfoArray = author?.Split(' ');
            var authorsFirstName = string.Empty;
            var authorsSecondName = string.Empty;

            if (authorsInfoArray != null)
            {
                authorsFirstName = authorsInfoArray.FirstOrDefault();
                if (authorsInfoArray.Length > 1) authorsSecondName = authorsInfoArray[1];
            }

            var result = new Book
            {
                Id = int.Parse(id),
                Cover = $@"http://wp8-ebook.litres.ru{img}",
                CoverPreview = $@"http://wp8-ebook.litres.ru{img}",
                Price = base_price,
                BasePrice = base_price,
                Description = new Book.TextDescription
                {
                    Hidden = new Book.Hidden
                    {
                        TitleInfo = new Book.TitleInfo
                        {
                            BookTitle = name,
                            Author = new [] {
                                new Book.TitleInfo.AuthorInfo
                                {
                                  FirstName = authorsFirstName, 
                                  LastName = authorsSecondName
                                }
                            }
                        }
                    }
                }
            };

            result.Rating = mark;
            result.Recenses = marks;

            if (!string.IsNullOrEmpty(freebie) && freebie.Equals("1"))
            {
                result.Categories = new Book.CategoriesInfo
                {
                    Categories = new XCollection<Book.Categorie>
                    {
                        new Book.Categorie
                        {
                            Id="4",
                            Name = "Free"
                        }
                    }
                };

            }

            return result;
        }
    }

    public class Author
    {
        public string totalaudio { get; set; }
        public string totalhardcopy { get; set; }
        public string url { get; set; }
        public string totaltext { get; set; }
        public string text { get; set; }
        public string arts_count { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string uuid { get; set; }
        public string img { get; set; }

        public Person ToPerson()
        {
            int artsCount = arts_count != null ? int.Parse(arts_count) : 0;
            var authorsInfoArray = name?.Split(' ');
            var authorsFirstName = string.Empty;
            var authorsSecondName = string.Empty;
            if (authorsInfoArray != null)
            {
                authorsFirstName = authorsInfoArray.FirstOrDefault();
                if (authorsInfoArray.Length > 1) authorsSecondName = authorsInfoArray[1];
            }

            return new Person
            {
                ArtsCount = artsCount,
                Id = uuid,
                Photo = img,
                Title = new Person.PersonTitle { Main = name },
                FirstName = authorsFirstName,
                LastName = authorsSecondName,
            };
        }
    }

    public class Genre
    {
        private string _name;
        public string img_w { get; set; }
        public string root_title { get; set; }
        public string sub_name { get; set; }
        public string url { get; set; }
        public string root_id { get; set; }
        public string id { get; set; }

        public string name
        {
            get { return _name?.ToUpper(); }
            set { _name = value; }
        }
    
        public string img { get; set; }
        public string img_h { get; set; }
    }

    public class Series
    {
        public string img { get; set; }
        public string img_h { get; set; }
        public string name { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public string total_arts { get; set; }
        public string url { get; set; }
        public string author { get; set; }
        public string img_w { get; set; }
    }

    public class Collection
    {
        public string url { get; set; }
        public string img_w { get; set; }
        public string text { get; set; }
        public string arts_count { get; set; }
        public string id { get; set; }
        public string img { get; set; }
        public string img_h { get; set; }
        public string name { get; set; }
    }

    public class Tag
    {
        private string _name;
        public string img_w { get; set; }
        public string url { get; set; }
        public string text { get; set; }
        public string arts_count { get; set; }
        public string id { get; set; }
        public string img_h { get; set; }
        public string img { get; set; }
        public string name
        {
            get { return _name?.ToUpper(); }
            set { _name = value; }
        }
    }

}
