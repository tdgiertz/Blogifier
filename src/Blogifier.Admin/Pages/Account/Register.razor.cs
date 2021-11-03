using Blogifier.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Blogifier.Admin.Pages.Account
{
	public partial class Register : ComponentBase
	{
        [Inject]
        public IConfiguration Configuration { get; set; }

        [CascadingParameter]
        private Task<AuthenticationState> authenticationStateTask { get; set; }

        private bool canShow = false;

		public bool showError = false;
		public RegisterModel model = new RegisterModel { Name = "", Email = "", Password = "", PasswordConfirm = "" };

        protected async override Task OnInitializedAsync()
        {
            var authState = await authenticationStateTask;

            if(!authState.User.Identity.IsAuthenticated && !Configuration.GetValue<bool>("Blogifier:UnauthenticatedRegisterEnabled"))
            {
                _navigationManager.NavigateTo("admin");
            }
            else
            {
                canShow = true;
            }
        }

		public async Task RegisterUser()
		{
			var result = await Http.PostAsJsonAsync<RegisterModel>("api/author/register", model);

			if (result.IsSuccessStatusCode)
			{
				showError = false;
				_navigationManager.NavigateTo("admin", true);
			}
			else
			{
				showError = true;
				StateHasChanged();
			}
		}
	}
}
