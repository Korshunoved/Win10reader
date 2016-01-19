using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Digillect.Mvvm.UI;

using LitRes;
using LitRes.Models;
using LitRes.ViewModels;

namespace LitRes.Views
{
	[View("FreeBooksByCategory")]
	[ViewParameter("category", typeof(int))]
	public partial class FreeBooksByCategory : FreeBooksByCategoryFitting
	{
		#region Constructors/Disposer
        public FreeBooksByCategory()
		{
			InitializeComponent();
		}
		#endregion

		#region CreateDataSession
		protected override Digillect.Mvvm.Session CreateDataSession( DataLoadReason reason )
		{
			ViewModel.BooksViewModelType =  ViewParameters.Get<int>( "category" );

			return base.CreateDataSession( reason );
		}
		#endregion
	}

    public class FreeBooksByCategoryFitting : ViewModelPage<FreeBooksByCategoryViewModel>
	{
	}
}