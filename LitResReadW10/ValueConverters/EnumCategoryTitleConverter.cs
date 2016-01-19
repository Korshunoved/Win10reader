using LitRes.ViewModels;

namespace LitRes.ValueConverters
{
	public class EnumCategoryTitleConverter : ConverterBase<int, string>
	{
		public override object Convert(int value, object parameter, string language)
        {
			switch ( (BooksByCategoryViewModel.BooksViewModelTypeEnum)value )
			{
				case BooksByCategoryViewModel.BooksViewModelTypeEnum.Interesting:
					return "ИНТЕРЕСНЫЕ";
				case BooksByCategoryViewModel.BooksViewModelTypeEnum.Novelty:
					return "НОВИНКИ";
				case BooksByCategoryViewModel.BooksViewModelTypeEnum.Popular:
					return "ПОПУЛЯРНЫЕ";
				case BooksByCategoryViewModel.BooksViewModelTypeEnum.NokiaCollection:
					return "ПОЛКА LUMIA";
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.FreeBooks:
                    return "БЕСПЛАТНЫЕ";
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Sequense:
                    return "СЕРИЯ";
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Tags:
                    return "ТЕГИ";
                case BooksByCategoryViewModel.BooksViewModelTypeEnum.Genre:
                    return "ЖАНРЫ";
                default:
					return "РЕКОМЕНДУЕМ";
			}
		}
	}
}
