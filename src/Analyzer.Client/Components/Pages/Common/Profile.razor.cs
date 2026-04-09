using Microsoft.AspNetCore.Components;
using MudBlazor;

using Analyzer.Shared.DTO;
using Analyzer.Domain.Enums;

namespace Analyzer.Client.Components.Pages.Common;

public partial class Profile : ComponentBase
{
    private bool _isLoading = true;
    private UserDto? _user;

    private bool _isSavingProfile = false;
    private bool _isSavingPassword = false;

    // Модели форм
    private UpdateProfileViewModel _profileModel = new();
    private ChangePasswordViewModel _passwordModel = new();

    private bool _showOldPassword;
    private InputType _oldPasswordInput = InputType.Password;
    private string _oldPasswordIcon = Icons.Material.Filled.VisibilityOff;

    private bool _showNewPassword;
    private InputType _newPasswordInput = InputType.Password;
    private string _newPasswordIcon = Icons.Material.Filled.VisibilityOff;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _user = await Http.GetFromJsonAsync<UserDto>("api/v1/users/me");
            if (_user is not null)
            {
                _profileModel.Username = _user.Username;
                _profileModel.Email = _user.Email;
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task UpdateProfileAsync()
    {
        _isSavingProfile = true;
        try
        {
            var response = await Http.PutAsJsonAsync("api/v1/users/me/profile", _profileModel);
            
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Профиль успешно обновлен", Severity.Success);
                if (_user is not null)
                {
                    _user.Username = _profileModel.Username;
                    _user.Email = _profileModel.Email;
                }
            }
        }
        finally
        {
            _isSavingProfile = false;
        }
    }

    private async Task ChangePasswordAsync()
    {
        _isSavingPassword = true;
        try
        {
            var request = new {
                _passwordModel.OldPassword,
                _passwordModel.NewPassword 
            };

            var response = await Http.PutAsJsonAsync("api/v1/users/me/password", request);

            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Пароль успешно изменен", Severity.Success);
                _passwordModel = new ChangePasswordViewModel();
            }
        }
        finally
        {
            _isSavingPassword = false;
        }
    }


    private Color GetRoleColor(Role role) => role switch
    {
        Role.Admin => Color.Error,
        Role.Architect => Color.Secondary,
        Role.SRE => Color.Warning,
        _ => Color.Info
    };

    private void ToggleOldPasswordVisibility()
    {
        if (_showOldPassword)
        {
            _showOldPassword = false;
            _oldPasswordIcon = Icons.Material.Filled.VisibilityOff;
            _oldPasswordInput = InputType.Password;
        }
        else
        {
            _showOldPassword = true;
            _oldPasswordIcon = Icons.Material.Filled.Visibility;
            _oldPasswordInput = InputType.Text;
        }
    }

    private void ToggleNewPasswordVisibility()
    {
        if (_showNewPassword)
        {
            _showNewPassword = false;
            _newPasswordIcon = Icons.Material.Filled.VisibilityOff;
            _newPasswordInput = InputType.Password;
        }
        else
        {
            _showNewPassword = true;
            _newPasswordIcon = Icons.Material.Filled.Visibility;
            _newPasswordInput = InputType.Text;
        }
    }
}