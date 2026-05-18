using System;
using System.Threading.Tasks;
using System.Windows.Input;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;

namespace LotteryDetectionMobile.ViewModel;

[QueryProperty(nameof(Token), "Token")]
public class AcceptInvitationViewModel : BaseViewModel
{
    private readonly IFamilyService _familyService;
    private readonly INavigationService _navigation;
    private string _token = string.Empty;
    private string _errorMessage = string.Empty;

    public AcceptInvitationViewModel(INavigationService navigation, IFamilyService familyService)
    {
        _navigation = navigation;
        _familyService = familyService;
        AcceptCommand = new Command(async () => await AcceptAsync(), () => !IsBusy);
    }

    public string Token
    {
        get => _token;
        set => SetProperty(ref _token, value ?? string.Empty);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value ?? string.Empty);
    }

    public ICommand AcceptCommand { get; }

    public async Task AcceptAsync()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = "Please enter an invitation code.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var member = await _familyService.AcceptInvitationAsync(Token.Trim());
            if (member != null)
                await _navigation.NavigateToAdminAsync();
            else
                ErrorMessage = "Invitation not found or has expired. Please request a new invite.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Something went wrong. Please try again.";
            Console.WriteLine($"[AcceptInvitationViewModel] AcceptAsync failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
