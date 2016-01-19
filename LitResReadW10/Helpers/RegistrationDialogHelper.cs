
using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Digillect.Mvvm.Services;
using LitRes.Models;
using LitRes.Services.Connectivity;
using LitResReadW10;

namespace LitRes.Helpers
{
    public class RegistrationDialogHelper
    {
        #region Events

        public event Action CancelClicked;

        private void InvokeCancelClicked()
        {
            if (CancelClicked != null)
            {
                CancelClicked();
            }
        }

        #endregion

        #region Public Methods

        public async Task<bool> ShowDialog()
        {
            CatalitCredentials cred = null;
            cred = await (App.Current as App).Scope.Resolve<ICredentialsProvider>().ProvideCredentials(CancellationToken.None);
            if (cred == null || cred.IsRealAccount) return false;
            if (cred.PurchasedBooksCount == 0) return false;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var _navigationService =  (App.Current as App).Scope.Resolve<INavigationService>();
                var messageBox = new RegistrationMessageBox();
                messageBox.LoginButtonClicked += delegate
                {
                    _navigationService.Navigate("Authorization", Parameters.From("returnBackToBook", true));
                };
                messageBox.RegisterButtonClicked += delegate
                {
                    _navigationService.Navigate("Registration", new Parameters{{"uri", "uri"},{"returnBackToBook", true}});
                };
                messageBox.CancelButtonClicked += InvokeCancelClicked;
                messageBox.Show();
            });
            return true;
        }

        #endregion
    }
}
