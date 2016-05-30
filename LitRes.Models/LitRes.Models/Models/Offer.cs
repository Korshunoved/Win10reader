using System.Diagnostics;
using System.Xml.Serialization;
using Digillect;
using Digillect.Collections;

namespace LitRes.Models.Models
{
    [XmlRoot("catalit-offers")]
    public class OffersResponse : XObject
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private XCollection<Offer> _offers;

        [XmlElement("offer")]
        public XCollection<Offer> Offers
        {
            get { return _offers; }
            set
            {
                if (!object.Equals(_offers, value))
                {
                    OnPropertyChangingOld("Offers", _offers, value);
                    _offers = value;
                    OnPropertyChanged("Offers");
                }
            }
        }
    }

    [XmlRoot("offer")]
    public class Offer : XObject
    {
        #region Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _id;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private int _campaign;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _added;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _addedTimestamp;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _validTillTimestamp;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private int _viewsCount;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private int _class;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _validTill;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private OfferXml _xml;

        #endregion

        #region Properties

        [XmlAttribute("id")]
        public string Id
        {
            get { return _id; }
            set
            {
                if (Equals(_id, value)) return;
                OnPropertyChangingOld("Id", _id, value);
                _id = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute("campaign")]
        public int Campaign
        {
            get { return _campaign; }
            set
            {
                if (Equals(_campaign, value)) return;
                OnPropertyChangingOld("Campaign", _campaign, value);
                _campaign = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute("added")]
        public string Added
        {
            get { return _added; }
            set
            {
                if (!object.Equals(_added, value))
                {
                    OnPropertyChangingOld("Added", _added, value);
                    _added = value;
                    OnPropertyChanged("Added");
                }
            }
        }

        [XmlAttribute("added_timestamp")]
        public string AddedTimestamp
        {
            get { return _addedTimestamp; }
            set
            {
                if (!object.Equals(_addedTimestamp, value))
                {
                    OnPropertyChangingOld("AddedTimestamp", _addedTimestamp, value);
                    _addedTimestamp = value;
                    OnPropertyChanged("AddedTimestamp");
                }
            }
        }

        [XmlAttribute("valid_till_timestamp")]
        public string ValidTillTimestamp
        {
            get { return _validTillTimestamp; }
            set
            {
                if (!object.Equals(_validTillTimestamp, value))
                {
                    OnPropertyChangingOld("ValidTillTimestamp", _validTillTimestamp, value);
                    _validTillTimestamp = value;
                    OnPropertyChanged("ValidTillTimestamp");
                }
            }
        }

        [XmlAttribute("views_count")]
        public int ViewsCount
        {
            get { return _viewsCount; }
            set
            {
                if (!object.Equals(_viewsCount, value))
                {
                    OnPropertyChangingOld("ViewsCount", _viewsCount, value);
                    _viewsCount = value;
                    OnPropertyChanged("ViewsCount");
                }
            }
        }

        [XmlAttribute("class")]
        public int Class
        {
            get { return _class; }
            set
            {
                if (!object.Equals(_class, value))
                {
                    OnPropertyChangingOld("Class", _class, value);
                    _class = value;
                    OnPropertyChanged("Class");
                }
            }
        }

        [XmlAttribute("valid_till")]
        public string ValidTill
        {
            get { return _validTill; }
            set
            {
                if (!object.Equals(_validTill, value))
                {
                    OnPropertyChangingOld("ValidTill", _validTill, value);
                    _validTill = value;
                    OnPropertyChanged("ValidTill");
                }
            }
        }

        [XmlElement("xml")]
        public OfferXml Xml
        {
            get { return _xml; }
            set
            {
                if (!object.Equals(_xml, value))
                {
                    OnPropertyChangingOld("Xml", _xml, value);
                    _xml = value;
                    OnPropertyChanged("Xml");
                }
            }
        }

        #endregion
    }

    [XmlRoot("xml")]
    public class OfferXml : XObject
    {
        [XmlElement("hidden")]
        public HiddenOffer Hidden { get; set; }
    }

    [XmlRoot("hidden")]
    public class HiddenOffer : XObject
    {
        [XmlElement("nthpresent")]
        public Nthpresent Present { get; set; }
    }

    [XmlRoot("nthpresent")]
    public class Nthpresent : XObject
    {
        [XmlAttribute("art")]
        public string Art { get; set; }

        [XmlAttribute("present_price")]
        public double Price { get; set; }
    }
}
