using System;

namespace LitRes
{
    public class Analytics
    {
        //transitions
        public const string ViewMainPage = "MainPage";              //Просмотр главной магазина
        public const string ViewInteresting = "Interesting";           //Просмотр ленты “интересное”
        public const string ViewPop = "Pop";                   //Просмотр ленты “популярное”
        public const string ViewNew = "New";                   //Просмотр ленты “новинки”
        public const string ViewGenres = "Genres";                //Просмотр корня жанрового дерева
        public const string ViewGenres1Pop = "Genres1_Pop";           //Просмотр жанра 1ого уровня по популярности
        public const string ViewGenres1New = "Genres1_New";           //Просмотр жанра 1ого уровня по новизне
        public const string ViewGenres2Pop = "Genres2_Pop";           //Просмотр жанра 2ого уровня по популярности
        public const string ViewGenres2New = "Genres2_New";           //Просмотр жанра 2ого уровня по новизне
        public const string ViewSearch = "Search";                //Поиск
        public const string ViewBookcard = "Bookcard";              //Просмотр карточки книги
        public const string ViewRecenses = "Recenses";              //Просмотр отзывов
        public const string ViewAuthorBio = "AuthorBio";             //Просмотр карточки автора (Биография)
        public const string ViewAuthorNew = "AuthorNew";             //Просмотр карточки автора (Новинки)
        public const string ViewAuthorPop = "AuthorPop";             //Просмотр карточки автора (Популярные)
        public const string ViewMyBooks = "MyBooks";               //Просмотр раздела мои книги
        public const string ViewSettings = "Settings";              //Просмотр настроек приложения
        public const string ViewReader = "Reader";                //Читалка
        public const string ViewSettingsReader = "SettingsReader";        //Просмотр настроек читалки
        public const string ViewTOC = "TOC";                   //Просмотр оглавления
        public const string ViewHightkights = "Highlights";            //Просмотр заметок
        public const string ViewBookmarks = "Bookmarks";             //Просмотр закладок
        public const string ViewAbout = "About";                 //Просмотр “о программе”
        public const string ViewSubscriptions = "Subscriptions";         //Просмотр листа подписок

        //actions
        public const string ActionReadFragment = "Action_ReadFragment";        //Чтение фрагмента
        public const string ActionReadFull = "Action_ReadFull";            //Чтение полной книги
        public const string ActionGetFree = "Action_GetFree";             //Получение бесплатной книги 
        public const string ActionBuyFullCard = "Action_BuyFromFullCard";     //Покупка с полноценной карточки
        public const string ActionBuyFullCardOk = "Action_BuyFromFullCard_OK";  //Успех покупки с полноценной карточки
        public const string ActionBuyFromFragment = "Action_BuyFromFragment";     //Покупка из фрагмента
        public const string ActionBuyFrinFragmentOk = "Action_BuyFromFragment_OK";  //Успех покупки из фрагмента
        public const string ActionBuyFromUpsale = "Action_BuyFromUpsale";       //Покупка из upsale-блока в конце книги
        public const string ActionBuyFromUpsaleOk = "Action_BuyFromUpsale_OK";    //Покупка из блока “читают вместе с этим”
        public const string ActionGotoGenre = "Action_GotoGenre";           //Переход с карточки книги в жанр
        public const string ActionGotoSequence = "Action_GotoSequence";        //Переход с карточки книги в серию 
        public const string ActionGotoAuthor = "Action_GotoAuthor";          //Переход с карточки книги в автора 
        public const string ActionWriteReview = "Action_WriteReview";         //Написание отзыва
        public const string ActionWriteReviewOk = "Action_WriteReview_OK";      //Успех написания отзыва
        public const string ActionGotoBaner = "Action_GotoBanner";          //Переход по баннеру
        public const string ActionGotoLitres = "Action_GotoLitres";          //Переход на пополнение счета по карте
        public const string ActionSubscribe = "Action_Subscribe";           //Подписка на новинки автора 

        private static readonly Analytics instance = new Analytics();

        #region Properties
        public static Analytics Instance { get { return instance; } }
        #endregion

        #region Constructors
        protected Analytics() { }
        #endregion

        #region Methods
        public void sendMessage(string msg)
        {
            try
            {
                if (msg.StartsWith("Action_"))
                {
                   GoogleAnalytics.EasyTracker.GetTracker().SendEvent("Action", msg, null, 0);
                }
                else
                {
                   GoogleAnalytics.EasyTracker.GetTracker().SendView(msg);
                }
            }
            catch (Exception e)
            {}
        }
        #endregion        
    }
}
