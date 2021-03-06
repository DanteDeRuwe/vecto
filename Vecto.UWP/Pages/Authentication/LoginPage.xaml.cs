using Refit;
using System;
using System.Net.Http;
using Vecto.Application.Login;
using Vecto.UWP.Services;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Vecto.UWP.Pages.Authentication
{
    public sealed partial class LoginPage : Page
    {
        private readonly IApiService _service;
        private readonly PasswordVault _passwordVault;

        public LoginPage()
        {
            _service = CustomRefitService.For<IApiService>();
            _passwordVault = new PasswordVault();
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // check if navigated from register, if so already fill in the registered e-mail
            if (e.Parameter is string && !e.Parameter.ToString().Equals(""))
            {
                EmailTextBox.Text = (string) e.Parameter;
                PasswordBox.Password = "";
                RememberMe.IsChecked = false;
            }
            else
            {
                // automatic login if credential found (remember me was checked previously)
                var credential = GetCredential();
                if (credential != null)
                {
                    credential.RetrievePassword();

                    EmailTextBox.Text = credential.UserName;
                    PasswordBox.Password = credential.Password;
                    RememberMe.IsChecked = true;

                    AttemptLogin(credential.UserName, credential.Password, true);
                }
            }

            EmailTextBox.Select(EmailTextBox.Text.Length, 0); // move cursor to end of textbox
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin(EmailTextBox.Text, PasswordBox.Password, RememberMe.IsChecked ?? false);
        }

        private async void AttemptLogin(string email, string password, bool rememberMe)
        {
            LoginButton.IsEnabled = false;
            try
            {
                LoginProgressRing.Visibility = Visibility.Visible;

                var model = new LoginDTO() { Email = email, Password = password };

                string token = await _service.Login(model);

                ClearCredential();
                if (rememberMe) StoreCredential(model);

                StoreToken(token);

                Frame.Navigate(typeof(MainPage));
            }
            catch (ApiException)
            {
                ErrorTextBlock.Text = "Username or password incorrect";
            }
            catch (HttpRequestException)
            {
                ErrorTextBlock.Text = "Can't connect to API";
            }
            finally
            {
                LoginProgressRing.Visibility = Visibility.Collapsed;
                LoginButton.IsEnabled = true;
            }
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RegisterPage), null, new SuppressNavigationTransitionInfo());
        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
                AttemptLogin(EmailTextBox.Text, PasswordBox.Password, RememberMe.IsChecked ?? false);
        }

        private void StoreCredential(LoginDTO model)
        {
            _passwordVault.Add(new PasswordCredential("Vecto", model.Email, model.Password));
        }

        private void ClearCredential()
        {
            if (GetCredential() != null) _passwordVault.Remove(GetCredential());
        }

        private void StoreToken(string token)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["token"] = token.Replace(@"""", "");
        }

        private PasswordCredential GetCredential()
        {
            try
            {
                var credentialEnumerator = _passwordVault.FindAllByResource("Vecto").GetEnumerator();
                credentialEnumerator.MoveNext();
                var credential = credentialEnumerator.Current;
                return credential;
            }
            catch
            {
                return null;
            }
        }
    }
}
