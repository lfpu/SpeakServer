using Microsoft.Extensions.DependencyInjection;
using Speaker.Pages;

namespace Speaker
{
    public partial class AppShell : Shell
    {
        public AppShell(LoginPage loginPage)
        {
            InitializeComponent();

            Items.Add(new ShellContent
            {
                Content = loginPage
            });
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        }
    }
}
