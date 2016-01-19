using System.Linq;
using System.Reflection;
using Autofac;

using Digillect.Mvvm;
using LitRes.LibraryTools;
using LitRes.Services;
using Module = Autofac.Module;

namespace LitRes
{
	public class ViewModelsModule : Module
	{
		protected override void Load( ContainerBuilder builder )
		{
			base.Load( builder );

			builder.RegisterType<GenresProvider>().As<IGenresProvider>().SingleInstance();
			builder.RegisterType<CatalogProvider>().As<ICatalogProvider>().SingleInstance();
			builder.RegisterType<CredentialsProvider>().As<LitRes.Services.Connectivity.ICredentialsProvider>().SingleInstance();
			builder.RegisterType<SearchHistoryProvider>().As<ISearchHistoryProvider>().SingleInstance();
			builder.RegisterType<PersonsProvider>().As<IPersonsProvider>().SingleInstance();
			builder.RegisterType<RecensesProvider>().As<IRecensesProvider>().SingleInstance();
			builder.RegisterType<ProfileProvider>().As<IProfileProvider>().SingleInstance();
			builder.RegisterType<CollectionsProvider>().As<ICollectionsProvider>().SingleInstance();
			builder.RegisterType<BookProvider>().As<IBookProvider>().SingleInstance();
			builder.RegisterType<BookmarksProvider>().As<IBookmarksProvider>().SingleInstance();
			builder.RegisterType<NotificationsProvider>().As<INotificationsProvider>().As<IStartable>().SingleInstance();
			builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
			builder.RegisterType<LitresPurchaseService>().As<ILitresPurchaseService>().SingleInstance();
			builder.RegisterType<FileDownloadService>().As<IFileDownloadService>().SingleInstance();
            builder.RegisterType<ExpirationGuardian>().As<IExpirationGuardian>().SingleInstance();

			builder.RegisterAssemblyTypes( this.ThisAssembly )
			  .AssignableTo<ViewModel>()
			  .Where( t => !t.GetTypeInfo().IsAbstract && t.GetTypeInfo().GetCustomAttributes( typeof( SingletonViewModelAttribute ), false ).Count() == 0 )
			  .AsSelf()
			  .OwnedByLifetimeScope()
			  .PropertiesAutowired();

			builder.RegisterAssemblyTypes( ThisAssembly )
			 .AssignableTo<ViewModel>()
			 .Where( t => !t.GetTypeInfo().IsAbstract && t.GetTypeInfo().GetCustomAttributes( typeof( SingletonViewModelAttribute ), false ).Count() != 0 )
			 .AsSelf()
			 .OwnedByLifetimeScope()
			 .PropertiesAutowired()
			 .SingleInstance();
		}
	}
}
