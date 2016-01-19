/// <reference path="Site/FB3ReaderSite.ts" />
/// <reference path="Reader/FB3Reader.ts" />
/// <reference path="DOM/FB3DOM.ts" />
/// <reference path="DataProvider/FB3AjaxDataProvider.ts" />
/// <reference path="Bookmarks/FB3Bookmarks.ts" />
/// <reference path="PagesPositionsCache/PPCache.ts" />
/// <reference path="Site/LocalBookmarks.ts" />
var AFB3Reader;
var AFB3PPCache;
var BookmarksProcessor;
var start;
var LocalArtID = 11668997;
var ColumnsCount = 1;
var Temp = 0;
var LitresLocalBookmarks = new LocalBookmarks.LocalBookmarksClass(LocalArtID.toString());
var aldebaran_or4 = false;
var Spacing;
var FontFace;
var FontSize;
window.onload = function () {
    // document.getElementById('reader').addEventListener('touchstart', TapStart, false);
    // document.getElementById('reader').addEventListener('touchmove', TapMove, false);
    // document.getElementById('reader').addEventListener('touchend', TapEnd, false);
    // var Version = '1.2';
    // var UUID = '43e7a504-33d4-4f37-b715-410342955f1f';
    // var SID = GetSID();
    // var Canvas = document.getElementById('reader');
    // var AReaderSite = new FB3ReaderSite.ExampleSite(Canvas);
    // var DataProvider = new FB3DataProvider.AJAXDataProvider(GetBaseURL(), ArtID2URL);
    // AFB3PPCache = new FB3PPCache.PPCache();
    // var AReaderDOM = new FB3DOM.DOM(AReaderSite, AReaderSite.Progressor, DataProvider, AFB3PPCache);
    // BookmarksProcessor = new FB3Bookmarks.LitResBookmarksProcessor(AReaderDOM, LocalArtID.toString(), SID, LitresLocalBookmarks.GetCurrentArtBookmarks());
    // BookmarksProcessor.FB3DOM.Bookmarks.push(BookmarksProcessor);
    // AFB3Reader = new FB3Reader.Reader(UUID, true, AReaderSite, AReaderDOM, BookmarksProcessor, Version, AFB3PPCache);
    // AFB3Reader.HyphON = !(/Android [12]\./i.test(navigator.userAgent)); // Android 2.* is unable to work with soft hyphens properly
    // PrepareCSS();
    // AFB3Reader.CanvasReadyCallback = function () {
        // document.getElementById('REnderEnd').innerHTML = (Temp++).toString();
    // };
    // AFB3Reader.Init([4417]);
    // // AFB3Reader.Init(LitresLocalBookmarks.GetCurrentPosition()); // start from localBookmarks
    // window.addEventListener('resize', function () { return AFB3Reader.AfterCanvasResize(); });
    // //	ShowPosition();
    // start = new Date().getTime();
};
function RunReader(currentArtId, columns) {

    LocalArtID = currentArtId;
    ColumnsCount = columns;

	document.getElementById('reader').addEventListener('touchstart', TapStart, false);
    document.getElementById('reader').addEventListener('touchmove', TapMove, false);
    document.getElementById('reader').addEventListener('touchend', TapEnd, false);
    document.getElementById('reader').addEventListener('mousedown', TapStart, false);
    document.getElementById('reader').addEventListener('mousemove', TapMove, false);
    document.getElementById('reader').addEventListener('mouseup', TapEnd, false);

    var Version = '1.2';
    var UUID = '43e7a504-33d4-4f37-b715-410342955f1f';
    var SID = GetSID();
    var Canvas = document.getElementById('reader');
    var AReaderSite = new FB3ReaderSite.ExampleSite(Canvas);
    var DataProvider = new FB3DataProvider.AJAXDataProvider(GetBaseURL(), ArtID2URL);
    AFB3PPCache = new FB3PPCache.PPCache();
    var AReaderDOM = new FB3DOM.DOM(AReaderSite, AReaderSite.Progressor, DataProvider, AFB3PPCache);
    BookmarksProcessor = new FB3Bookmarks.LitResBookmarksProcessor(AReaderDOM, LocalArtID.toString(), SID, LitresLocalBookmarks.GetCurrentArtBookmarks());
    BookmarksProcessor.FB3DOM.Bookmarks.push(BookmarksProcessor);
    AFB3Reader = new FB3Reader.Reader(UUID, true, AReaderSite, AReaderDOM, BookmarksProcessor, Version, AFB3PPCache);
    AFB3Reader.HyphON = !(/Android [12]\./i.test(navigator.userAgent)); // Android 2.* is unable to work with soft hyphens properly
    PrepareCSS();
    AFB3Reader.CanvasReadyCallback = function () {
        document.getElementById('REnderEnd').innerHTML = (Temp++).toString();
    };
    
    AFB3Reader.Init([0]);
    // AFB3Reader.Init(LitresLocalBookmarks.GetCurrentPosition()); // start from localBookmarks
    window.addEventListener('resize', function () { return AFB3Reader.AfterCanvasResize(); });
    //	ShowPosition();
    start = new Date().getTime();
}

function ArtID2URL(Chunk) {
    var OutURL = '/MyBooks/' + LocalArtID + '/';
    if (Chunk == null) {
        OutURL += 'toc.js';
    }
    else if (Chunk.match(/\./)) {
        OutURL += Chunk;
    }
    else {
        OutURL += FB3DataProvider.zeroPad(Chunk, 3) + '.js';
    }
    return OutURL;
}

function PagesCount() {
    return "" + AFB3Reader.PagesPositionsCache.LastPage();
}

function GetXPointer() {
    return ""+AFB3Reader.FB3DOM.GetXPathFromPos(AFB3Reader.CurStartPos);
    //return "fb2#xpointer(point(/1/2/1/" + AFB3Reader.CurStartPos.join('/')+".0))";
}

function CurrentPage() {
    return "" + AFB3Reader.CurStartPage;
}

function GetCurrentPercent() {
    return "" + AFB3Reader.CurPosPercent() ? AFB3Reader.CurPosPercent().toFixed(2) : "";
}

function GetCurrentBookmarkInfo() {
    try {
        MakeNewNote();
      
        NativeNote = NativeNote.RoundClone(true);
        
        DialogBookmark = NativeNote;
        BookmarksProcessor.AddBookmark(DialogBookmark);
        return "" + DialogBookmark.Title + "|" + DialogBookmark.RawText;
    } catch (ex) {
        return ex.message;
    }
}

function GetSID() {
    var URL = decodeURIComponent(window.location.href);
    var SID = URL.match(/\bsid=([0-9a-zA-Z]+)\b/);
    if (SID == null || !SID.length) {
        var Cookies = document.cookie.match(/(?:(?:^|.*;\s*)SID\s*\=\s*([^;]*).*$)|^.*$/);
        if (!Cookies.length) {
            return 'null';
        }
        return Cookies[1];
    }
    else {
        return SID[1];
    }
}
function GetBaseURL() {
    var URL = decodeURIComponent(window.location.href);
    var BaseURL = URL.match(/\bbaseurl=([0-9\/a-z\.]+)/i);
    if (BaseURL == null || !BaseURL.length) {
        return 'null';
    }
    return BaseURL[1];
}
var MarkupProgress;
var NativeNote;
var RoundedNote;
var DialogShown;
var TouchMoving = false;
var TouchData;
function TapStart(e) {
    //	e.preventDefault();
    TouchMoving = false;
    try {
        TouchData = e.touches[0];
    } catch (err) {
        TouchData = e;
    }
}
function TapMove(e) {
    //	e.preventDefault();
    TouchMoving = true;
}
function TapEnd(e) {
    e.preventDefault();
    if (!TouchMoving) {
        if (TouchData.pageX * 1 < screen.width * 0.4) {
            Pagebackward();
        }
        else if (TouchData.pageX * 1 > screen.width * 0.6) {
            PageForward();
        } else {
            window.external.notify("TapEndOnScreenCenter");
        }
        return false;
    }
}
var StartElPos;
function InitNote(NoteType) {
    if (NoteType == 'note') {
        MarkupProgress = 'selectstart';
        NativeNote.Group = 3;
    }
    else {
        RoundedNote = undefined;
        UpdateRange(StartElPos, StartElPos);
        NativeNote = NativeNote.RoundClone(true);
        NativeNote.Group = 1;
        document.getElementById('wholepara').disabled = true;
        document.getElementById('wholepara').checked = true;
        AFB3Reader.Redraw();
        ShowDialog(NativeNote);
    }
    HideMenu();
}
var Coords = false;
function MouseMove(Evt) {
    if (NativeNote && NativeNote.Group == 3 && !MenuShown && !DialogShown) {
        var X = Evt.clientX;
        var Y = Evt.clientY;
        // hack for touch-based devices
        if (!isRelativeToViewport())
            X += window.pageXOffset, Y += window.pageYOffset;
        var CurrCoords = { X: X, Y: Y };
        if (Coords) {
            CurrCoords = Coords;
        }
        Coords = false;
        var CurrentElPos = AFB3Reader.ElementAtXY(CurrCoords.X, CurrCoords.Y);
        if (CurrentElPos && CurrentElPos.length && StartElPos && StartElPos.length) {
            if (FB3Reader.PosCompare(CurrentElPos, StartElPos) < 0) {
                UpdateRange(CurrentElPos, StartElPos);
            }
            else {
                UpdateRange(StartElPos, CurrentElPos);
            }
            // logic - remove old one, create new, add new
            var NewNote = NativeNote.RoundClone(false);
            NewNote.TemporaryState = 1;
            NativeNote.Detach();
            NativeNote = NewNote;
            BookmarksProcessor.AddBookmark(NativeNote);
            AFB3Reader.RedrawVisible();
        }
    }
}
function UpdateRange(StartPos, EndPos) {
    NativeNote.Range.From = StartPos;
    NativeNote.Range.To = EndPos;
}
function FinishNote() {
    NativeNote.Detach();
    HideMenu();
    ShowDialog(NativeNote);
}
function CancelNote(NoDestroy) {
    if (!NoDestroy) {
        NativeNote.Detach();
    }
    MarkupProgress = undefined;
    NativeNote = undefined;
    HideMenu();
    AFB3Reader.Redraw();
}
var MenuShown;
function MakeNewNote() {
    if (NativeNote) {
        NativeNote.Detach();
    }
    if (!NativeNote) {
        NativeNote = new FB3Bookmarks.Bookmark(BookmarksProcessor);
    }
    NativeNote.TemporaryState = 1;
}
function ShowMenu(e) {
    HideDialog();
    MakeNewNote();
    var X = e.clientX;
    var Y = e.clientY;
    // hack for touch-based devices
    if (!isRelativeToViewport())
        X += window.pageXOffset, Y += window.pageYOffset;
    Coords = { X: X, Y: Y };
    StartElPos = AFB3Reader.ElementAtXY(Coords.X, Coords.Y);
    if (MarkupProgress == 'selectstart') {
        MenuShown = 'SelectEnd';
    }
    else {
        MenuShown = 'SelectStart';
    }
    var posx = X + (3 + window.pageXOffset) + 'px'; //Left Position of Mouse Pointer
    var posy = Y + (3 + window.pageYOffset) + 'px'; //Top Position of Mouse Pointer
    document.getElementById(MenuShown).style.position = 'absolute';
    document.getElementById(MenuShown).style.display = 'inline';
    document.getElementById(MenuShown).style.left = posx;
    document.getElementById(MenuShown).style.top = posy;
    return true;
}
function HideMenu() {
    if (MenuShown) {
        document.getElementById(MenuShown).style.display = 'none';
        MenuShown = undefined;
    }
}
function FinishAll() {
    CancelNote(true);
    HideDialog();
}
function DestroyBookmark() {
    NativeNote.Detach();
    DialogBookmark.Detach();
    FinishAll();
    AFB3Reader.Redraw();
}
function HideAll() {
    HideMenu();
    HideDialog();
}
var DialogBookmark;
function ShowDialog(Bookmark) {
    DialogBookmark = Bookmark;
    BookmarksProcessor.AddBookmark(DialogBookmark);
    document.getElementById('FromXPath').innerHTML = '/' + DialogBookmark.XStart.join('/');
    document.getElementById('ToXPath').innerHTML = '/' + DialogBookmark.XEnd.join('/');
    document.getElementById('notetitle').value = DialogBookmark.Title;
    document.getElementById('notedescr').value = DialogBookmark.RawText;
    document.getElementById('notetype').value = DialogBookmark.Group.toString();
    document.getElementById('notedescr').disabled = DialogBookmark.Group == 1 ? true : false;
    document.getElementById('sellwhole').style.display = Bookmark.ID ? 'none' : 'block';
    document.getElementById('notedialog').style.display = 'block';
    DialogShown = true;
}
function RoundNoteUp() {
    DialogBookmark.Detach();
    if (document.getElementById('wholepara').checked) {
        if (!RoundedNote) {
            RoundedNote = DialogBookmark.RoundClone(true);
        }
        ShowDialog(RoundedNote);
    }
    else {
        ShowDialog(NativeNote);
    }
    AFB3Reader.Redraw();
}
function HideDialog() {
    document.getElementById('notedialog').style.display = 'none';
    document.getElementById('wholepara').checked = false;
    document.getElementById('wholepara').disabled = false;
    DialogShown = false;
}
function ShowPosition() {
    window.external.notify("UpdatePagePosition");
    document.getElementById('CurPos').innerHTML = AFB3Reader.CurStartPos.join('/');
    document.getElementById('CurPosPercent').innerHTML = AFB3Reader.CurPosPercent() ? AFB3Reader.CurPosPercent().toFixed(2) : '?';
}
function PageForward() {
    AFB3Reader.PageForward();
    ShowPosition();
}
function Pagebackward() {
    AFB3Reader.PageBackward();
    ShowPosition();
}
function GoToPercent() {
    AFB3Reader.GoToPercent(parseFloat(document.getElementById('gotopercent').value));
    ShowPosition();
}

function GoToPage(number) {
    AFB3Reader.GoTOPage(number);
    ShowPosition();
}

function GetTOC() {
    return JSON.stringify(AFB3Reader.TOC());
}

function ShowTOC() {
    document.getElementById('tocdiv').innerHTML = Toc2Div(AFB3Reader.TOC());
    document.getElementById('tocdiv').style.display = "block";
}
function Toc2Div(TOCS) {
    var Out = '';
    for (var J = 0; J < TOCS.length; J++) {
        var TOC = TOCS[J];
        Out += '<div class="tocitm">';
        if (TOC.bookmarks && TOC.bookmarks.g0) {
            Out += '>';
        }
        if (TOC.t) {
            Out += '<a href = "javascript:GoToc(' + TOC.s + ')" > '
                + TOC.t + '</a>';
        }
        if (TOC.c) {
            for (var I = 0; I < TOC.c.length; I++) {
                Out += Toc2Div([TOC.c[I]]);
            }
        }
        Out += '</div>';
    }
    return Out;
}
function GoToc(S) {
    AFB3Reader.GoTO([S]);
    CloseBookmarksList();
}
function Bookmark2Div(Bookmark) {
    return '<div class="bookmarkdiv"><div style="float:right"><a href="javascript:DropBookmark('
        + Bookmark.N + ')">[X]</a></div><a href="javascript:ShowBookmark('
        + Bookmark.N + ')">'
        + Bookmark.Title + '</a></div>';
}
function ShowBookmark(N) {
    AFB3Reader.GoTO(AFB3Reader.Bookmarks.Bookmarks[N].Range.From);
}
function ManageBookmarks() {
    document.getElementById('bookmarksmandiv').style.display = "block";
    var Bookmarks = '';
    for (var J = 1; J < AFB3Reader.Bookmarks.Bookmarks.length; J++) {
        Bookmarks += Bookmark2Div(AFB3Reader.Bookmarks.Bookmarks[J]);
    }
    document.getElementById('bookmarkslist').innerHTML = Bookmarks;
}
function CloseBookmarksList() {
    document.getElementById('tocdiv').style.display = "none";
    document.getElementById('bookmarksmandiv').style.display = "none";
}
function DropBookmark(I) {
    AFB3Reader.Bookmarks.Bookmarks[I].Detach();
    ManageBookmarks();
    AFB3Reader.Redraw();
}
function Save() {
    console.log('save button clicked');
    LitresLocalBookmarks.StoreBookmarks(BookmarksProcessor.MakeStoreXML());
    BookmarksProcessor.Store();
}
function Load() {
    console.log('load button clicked');
    BookmarksProcessor.ReLoad();
}
function RefreshVisible() {
    AFB3Reader.RedrawVisible();
}
function ClearCache() {
    if (FB3PPCache.CheckStorageAvail()) {
        localStorage.clear();
    }
    RefreshVisible();
}
function PrepareCSS() {
    Spacing = document.getElementById('spacing').value;
    FontFace = document.getElementById('fontface').value;
    FontSize = document.getElementById('fontsize').value;
    var Colors = document.getElementById('Colors').value.split('/');
    // Colors does not mater for page size, AFB3Reader.NColumns already used internally
    AFB3Reader.Site.Key = Spacing + ':' + FontFace + ':' + FontSize;
    AFB3Reader.NColumns = 1;
    changecss('#FB3ReaderHostDiv', 'line-height', Spacing);
    changecss('#FB3ReaderHostDiv', 'font-family', FontFace);
    changecss('#FB3ReaderHostDiv', 'font-size', FontSize + 'px');
    changecss('#FB3ReaderHostDiv', 'background-color', 'transparent');
    changecss('#FB3ReaderHostDiv', 'color', Colors[1]);
}
function ApplyStyle() {
    PrepareCSS();
    AFB3Reader.Reset();
}
// https://github.com/moll/js-element-from-point/blob/master/index.js
var relativeToViewport;
function isRelativeToViewport() {
    if (relativeToViewport != null)
        return relativeToViewport;
    var x = window.pageXOffset ? window.pageXOffset + window.innerWidth - 1 : 0;
    var y = window.pageYOffset ? window.pageYOffset + window.innerHeight - 1 : 0;
    if (!x && !y)
        return true;
    // Test with a point larger than the viewport. If it returns an element,
    // then that means elementFromPoint takes page coordinates.
    return relativeToViewport = !document.elementFromPoint(x, y);
}
function GoXPath(NewPos) {
    AFB3Reader.GoToXPath(NewPos);
}
function changecss(theClass, element, value) {
    //Last Updated on July 4, 2011
    //documentation for this script at
    //http://www.shawnolson.net/a/503/altering-css-class-attributes-with-javascript.html
    var cssRules;
    var doc = document;
    for (var S = 0; S < doc.styleSheets.length; S++) {
        try {
            doc.styleSheets[S].insertRule(theClass + ' { ' + element + ': ' + value + '; }', doc.styleSheets[S][cssRules].length);
        }
        catch (err) {
            try {
                doc.styleSheets[S].addRule(theClass, element + ': ' + value + ';');
            }
            catch (err) {
                try {
                    if (doc.styleSheets[S]['rules']) {
                        cssRules = 'rules';
                    }
                    else if (doc.styleSheets[S]['cssRules']) {
                        cssRules = 'cssRules';
                    }
                    else {
                    }
                    for (var R = 0; R < doc.styleSheets[S][cssRules].length; R++) {
                        if (doc.styleSheets[S][cssRules][R].selectorText == theClass) {
                            if (doc.styleSheets[S][cssRules][R].style[element]) {
                                doc.styleSheets[S][cssRules][R].style[element] = value;
                                break;
                            }
                        }
                    }
                }
                catch (err) { }
            }
        }
    }
}
//# sourceMappingURL=app.js.map