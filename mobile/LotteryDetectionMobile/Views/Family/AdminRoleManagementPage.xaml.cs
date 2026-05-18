using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Dialogs;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using LotteryDetectionMobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.Family;

public partial class AdminRoleManagementPage : ContentPage, IQueryAttributable
{
    private bool _openInviteOnAppear;

    public AdminRoleManagementPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Settings";

        var familyService = MauiProgram.Services?.GetService<IFamilyService>();
        var auditLogService = MauiProgram.Services?.GetService<IFamilyAuditLogService>();
        var memberCache = MauiProgram.Services?.GetService<IFamilyMemberCache>();
        var authService = MauiProgram.Services?.GetService<IAuthService>();
        if (familyService != null)
            BindingContext = new AdminRoleManagementViewModel(NavigationService.Default, familyService, auditLogService, memberCache, authService);

        AttachHandlers();
    }

    private AdminRoleManagementViewModel? ViewModel => BindingContext as AdminRoleManagementViewModel;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("OpenInvite", out var openObj) && openObj is bool open && open)
            _openInviteOnAppear = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AttachHandlers();
        if (ViewModel != null) await ViewModel.InitializeAsync();

        if (_openInviteOnAppear && ViewModel != null)
        {
            _openInviteOnAppear = false;
            ViewModel.InviteMemberCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (ViewModel != null)
        {
            ViewModel.OverflowRequested -= OnOverflowRequested;
            ViewModel.InviteSentRequested -= OnInviteSent;
            ViewModel.ResendInviteRequested -= OnResendInviteRequested;
        }
    }

    private void AttachHandlers()
    {
        if (ViewModel == null) return;
        ViewModel.OverflowRequested -= OnOverflowRequested;
        ViewModel.InviteSentRequested -= OnInviteSent;
        ViewModel.ResendInviteRequested -= OnResendInviteRequested;
        ViewModel.OverflowRequested += OnOverflowRequested;
        ViewModel.InviteSentRequested += OnInviteSent;
        ViewModel.ResendInviteRequested += OnResendInviteRequested;
    }

    private async void OnOverflowRequested(object? sender, FamilyMember member)
    {
        if (ViewModel == null) return;
        var picked = await DisplayActionSheet(
            member.Name,
            "Cancel",
            null,
            "Resend invite", "Remove member");
        if (picked == "Remove member")
        {
            var ok = await AppDialog.ShowConfirmAsync(
                title: "Remove member?",
                message: $"{member.Name} will lose access to the family board.",
                acceptText: "Remove",
                cancelText: "Cancel",
                danger: true,
                icon: "👤",
                iconBackground: "#FEE2E2");
            if (ok)
            {
                var removed = await ViewModel.RemoveMemberAsync(member);
                if (!removed)
                    await AppDialog.ShowAlertAsync(
                        title: "Remove failed",
                        message: "Could not remove this member. Please try again.");
            }
        }
        else if (picked == "Resend invite")
        {
            await ViewModel.ResendMemberInviteAsync(member);
            await AppDialog.ShowAlertAsync(
                title: "Invite sent",
                message: $"A fresh invite was sent to {member.Name}.");
        }
    }

    private async void OnResendInviteRequested(object? sender, Models.Family.PendingInvite invite)
    {
        await AppDialog.ShowAlertAsync(
            title: "Invite resent",
            message: $"A fresh invite was sent to {invite.Email}.");
    }

    private async void OnInviteSent(object? sender, Models.Family.PendingInvite invite)
    {
        await AppDialog.ShowAlertAsync(
            title: "Invite sent",
            message: $"An invite was sent to {invite.Email} as {invite.Role}.");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (ViewModel?.IsInviteSheetOpen == true)
        {
            ViewModel.IsInviteSheetOpen = false;
            return;
        }
        if (ViewModel?.IsRolePickerOpen == true)
        {
            ViewModel.IsRolePickerOpen = false;
            return;
        }
        await NavigationService.Default.NavigateBackAsync();
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.OnTabSelectedAsync(e.TabKey);
    }
}
