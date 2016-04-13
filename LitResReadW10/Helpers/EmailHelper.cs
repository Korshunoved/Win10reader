using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.UI.Xaml;
using Autofac;
using LitRes.Services;
using LitRes.Services.Connectivity;

namespace LitResReadW10.Helpers
{
    class EmailHelper
    {
        public static async void OpenEmailClientWithLitresInfo()
        {
            IDeviceInfoService _deviceInfoService = ((App)App.Current).Scope.Resolve<IDeviceInfoService>();

            EmailMessage emailMessage = new EmailMessage();

            var credentialsProvider = ((App)Application.Current).Scope.Resolve<ICredentialsProvider>();
            var creds =  credentialsProvider.ProvideCredentials(CancellationToken.None);

            var catalogProvider = ((App)Application.Current).Scope.Resolve<ICatalogProvider>();
            var books = await catalogProvider.GetMyBooksFromCache(CancellationToken.None);

            var bookProvider = ((App)Application.Current).Scope.Resolve<IBookProvider>();
            var exists = await bookProvider.GetExistBooks(CancellationToken.None);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("\r\n\r\n----- Техническая информация:\r\n");

            stringBuilder.AppendLine(string.Format("Версия приложения: ЛитРес: Читай! версии {0}\r\n", _deviceInfoService.ApplicationVersion));
            stringBuilder.AppendLine(string.Format("Версия ОС: Windows 10 версии {0}\r\n", _deviceInfoService.OsVersion));

            if (creds != null)
            {
                stringBuilder.AppendLine(string.Format("Логин: {0}\r\n", creds.Login));
            }

            if (books != null && books.Count > 0)
            {
                stringBuilder.AppendLine(string.Format("Последняя книга: {0}, {1}\r\n", books[0].Id, books[0].Description.Hidden.TitleInfo.BookTitle));
            }

            if (exists != null)
            {
                stringBuilder.AppendLine(string.Format("Всего книг на устройстве: {0}\r\n", exists.Count));
            }

            emailMessage.To.Add(new EmailRecipient("win10@litres.ru"));

            emailMessage.Body = stringBuilder.ToString();
            try
            {
                await EmailManager.ShowComposeNewEmailAsync(emailMessage);
            }
            catch (Exception e)
            {
                return;
            }
        }

        public static async void OpenEmailClientWithoutPayload()
        {
            IDeviceInfoService _deviceInfoService = ((App)App.Current).Scope.Resolve<IDeviceInfoService>();

            EmailMessage emailMessage = new EmailMessage();

            var credentialsProvider = ((App)Application.Current).Scope.Resolve<ICredentialsProvider>();
            var creds = credentialsProvider.ProvideCredentials(CancellationToken.None);

            var catalogProvider = ((App)Application.Current).Scope.Resolve<ICatalogProvider>();
            var books = await catalogProvider.GetMyBooksFromCache(CancellationToken.None);

            var bookProvider = ((App)Application.Current).Scope.Resolve<IBookProvider>();
            var exists = await bookProvider.GetExistBooks(CancellationToken.None);

            var stringBuilder = new StringBuilder();

            emailMessage.To.Add(new EmailRecipient("win10@litres.ru"));
            emailMessage.Body = stringBuilder.ToString();
            try
            {
                await EmailManager.ShowComposeNewEmailAsync(emailMessage);
            }
            catch (Exception e)
            {
                return;
            }
        }
    }
}
