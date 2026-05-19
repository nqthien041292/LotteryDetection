using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Services.Auth;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;

namespace LotteryDetection.Mobile.ViewModel;

public class AdminRoleManagementViewModel : TabNavigationViewModel
{
    // Fallback role palette used only when the backend `GET /api/mobile/family/members/roles`
    // endpoint is unreachable. Ids MUST match the backend FamilyRole enum names so that an
    // invite / role-change call still parses correctly without the backend redirector.
    private static readonly RoleOption[] FallbackRolePalette =
    {
        new() { Id = "Parent", Label = "Parent", Description = "Full access · admin",     TintColor = "#FEF3C7", ForegroundColor = "#B45309" },
        new() { Id = "Member", Label = "Member", Description = "Standard family member",  TintColor = "#E0EAFF", ForegroundColor = "#1E40AF" },
        new() { Id = "Child",  Label = "Kid",    Description = "Sees own tasks only",     TintColor = "#E1DDFE", ForegroundColor = "#4C1D95" },
        new() { Id = "Viewer", Label = "Viewer", Description = "Read-only access",        TintColor = "#D1F0EC", ForegroundColor = "#115E59" }
    };

    private IReadOnlyList<RoleOption> rolePalette = FallbackRolePalette;

    private readonly IFamilyService? familyService;
    private readonly IFamilyAuditLogService? auditLogService;
    private readonly IFamilyMemberCache? memberCache;
    private readonly IAuthService? authService;
    private string familyName = "Your family";
    private string joinedLabel = string.Empty;
    private string adminName = "Admin";
    private bool isRolePickerOpen;
    private FamilyMember? pickerMember;
    private string pickerMemberName = string.Empty;
    private bool isInviteSheetOpen;
    private string inviteEmail = string.Empty;
    private string inviteError = string.Empty;
    private RoleOption? selectedInviteRole;
    private bool hasLoaded;
    private bool isLoading;

    public AdminRoleManagementViewModel()
        : this(NavigationService.Default, null, null)
    {
    }

    public AdminRoleManagementViewModel(
        INavigationService navigationService,
        IFamilyService? familyService,
        IFamilyAuditLogService? auditLogService = null,
        IFamilyMemberCache? memberCache = null,
        IAuthService? authService = null)
        : base(navigationService)
    {
        this.familyService = familyService;
        this.auditLogService = auditLogService;
        this.memberCache = memberCache;
        this.authService = authService;

        Members = new ObservableCollection<FamilyMember>();
        PendingInvites = new ObservableCollection<PendingInvite>();
        AuditEntries = new ObservableCollection<AuditEntry>();
        RolePickerOptions = new ObservableCollection<RoleOption>();
        InviteRoleOptions = new ObservableCollection<RoleOption>();

        ChangeRoleCommand = new Command<FamilyMember>(OpenRolePicker);
        OpenRolePickerCommand = new Command<FamilyMember>(OpenRolePicker);
        CloseRolePickerCommand = new Command(CloseRolePicker);
        // Use untyped Command + manual cast: Command<RoleOption>.CanExecute returns
        // false whenever the bound CommandParameter is null or a boxed mismatch,
        // which silently swallows the tap with no log. The cast-inside pattern
        // accepts any parameter and rejects it explicitly inside the handler.
        PickRoleCommand = new Command(async o => await PickRoleAsync(o as RoleOption));
        OverflowCommand = new Command<FamilyMember>(member => OverflowRequested?.Invoke(this, member));
        ResendInviteCommand = new Command<PendingInvite>(async invite => await ResendInviteAsync(invite));
        InviteMemberCommand = new Command(OpenInviteSheet);
        CloseInviteSheetCommand = new Command(CloseInviteSheet);
        PickInviteRoleCommand = new Command(o => PickInviteRole(o as RoleOption));
        SendInviteCommand = new Command(async () => await SendInviteAsync());
    }

    public ObservableCollection<FamilyMember> Members { get; }
    public ObservableCollection<PendingInvite> PendingInvites { get; }
    public ObservableCollection<AuditEntry> AuditEntries { get; }
    public ObservableCollection<RoleOption> RolePickerOptions { get; }
    public ObservableCollection<RoleOption> InviteRoleOptions { get; }

    public ICommand ChangeRoleCommand { get; }
    public ICommand OpenRolePickerCommand { get; }
    public ICommand CloseRolePickerCommand { get; }
    public ICommand PickRoleCommand { get; }
    public ICommand OverflowCommand { get; }
    public ICommand ResendInviteCommand { get; }
    public ICommand InviteMemberCommand { get; }
    public ICommand CloseInviteSheetCommand { get; }
    public ICommand PickInviteRoleCommand { get; }
    public ICommand SendInviteCommand { get; }

    public string FamilyName
    {
        get => familyName;
        private set => SetProperty(ref familyName, value);
    }

    public string MemberCountText => string.IsNullOrEmpty(joinedLabel)
        ? $"{Members.Count} members"
        : $"{Members.Count} members · {joinedLabel}";

    public string AdminName
    {
        get => adminName;
        private set => SetProperty(ref adminName, value);
    }

    public bool IsRolePickerOpen
    {
        get => isRolePickerOpen;
        set => SetProperty(ref isRolePickerOpen, value);
    }

    public string PickerMemberName
    {
        get => pickerMemberName;
        private set => SetProperty(ref pickerMemberName, value);
    }

    public bool HasPendingInvites => PendingInvites.Count > 0;
    public bool HasMembers => Members.Count > 0;

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public bool IsInviteSheetOpen
    {
        get => isInviteSheetOpen;
        set => SetProperty(ref isInviteSheetOpen, value);
    }

    public string InviteEmail
    {
        get => inviteEmail;
        set => SetProperty(ref inviteEmail, value);
    }

    public string InviteError
    {
        get => inviteError;
        private set
        {
            if (SetProperty(ref inviteError, value))
                NotifyPropertyChanged(nameof(HasInviteError));
        }
    }

    public bool HasInviteError => !string.IsNullOrEmpty(InviteError);

    public event EventHandler<FamilyMember>? OverflowRequested;
    public event EventHandler<PendingInvite>? ResendInviteRequested;
    public event EventHandler<PendingInvite>? InviteSentRequested;

    public async Task InitializeAsync()
    {
        IsLoading = !hasLoaded;
        try
        {
            // Pull the canonical role palette from the backend so role ids stay
            // in sync with the FamilyRole enum. Falls back silently to the
            // hardcoded palette if the call fails — the fallback ids also match
            // backend enum names, so invites still parse correctly.
            if (familyService != null)
            {
                try
                {
                    var fetched = await familyService.GetRolesAsync();
                    if (fetched.Count > 0)
                        rolePalette = fetched;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] GetRolesAsync failed: {ex.Message}");
                }
            }

            var source = memberCache != null
                ? (IEnumerable<FamilyMember>?)await TryGetCachedMembersAsync()
                : familyService != null
                    ? await TryGetServiceMembersAsync()
                    : null;

            if (source != null)
            {
                var fetched = source.ToList();
                Members.Clear();
                PendingInvites.Clear();
                foreach (var m in fetched)
                {
                    if (m.IsPending)
                        PendingInvites.Add(new PendingInvite
                        {
                            MemberId = m.Id ?? string.Empty,
                            Email = string.IsNullOrEmpty(m.Email) ? m.Name : m.Email,
                            Role = m.Role ?? "Member",
                            InvitedAtRelative = "pending"
                        });
                    else
                        Members.Add(m);
                }
                NotifyPropertyChanged(nameof(HasPendingInvites));
                NotifyPropertyChanged(nameof(HasMembers));
            }

            if (familyService != null)
            {
                try
                {
                    var group = await familyService.GetGroupAsync();
                    if (group != null)
                    {
                        if (!string.IsNullOrWhiteSpace(group.Name))
                            FamilyName = group.Name;
                        joinedLabel = group.CreatedAt > DateTime.MinValue
                            ? $"joined {group.CreatedAt:MMM yyyy}"
                            : string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] GetGroupAsync failed: {ex.Message}");
                }
            }

            var displayName = authService?.UserDisplayName;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                var first = displayName.Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first))
                    AdminName = $"Admin · {char.ToUpperInvariant(first[0])}{first[1..]}";
            }

            NotifyPropertyChanged(nameof(MemberCountText));

            if (auditLogService != null)
            {
                try
                {
                    var entries = await auditLogService.GetAuditLogAsync();
                    AuditEntries.Clear();
                    foreach (var e in entries) AuditEntries.Add(e);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] GetAuditLogAsync failed: {ex.Message}");
                }
            }
        }
        finally
        {
            hasLoaded = true;
            IsLoading = false;
        }
    }

    public Task OnTabSelectedAsync(string? tabKey)
    {
        return HandleTabSelectionAsync(tabKey);
    }

    public void ApplyRoleChange(FamilyMember member, string role)
    {
        var index = Members.IndexOf(member);
        if (index < 0) return;
        member.Role = role;
        Members.RemoveAt(index);
        Members.Insert(index, member);
    }

    public async Task<bool> RemoveMemberAsync(FamilyMember member)
    {
        if (familyService != null && !string.IsNullOrEmpty(member.Id))
        {
            try
            {
                var ok = await familyService.RemoveMemberAsync(member.Id);
                if (!ok) return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] RemoveMemberAsync failed: {ex.Message}");
                return false;
            }
        }
        memberCache?.Invalidate();
        Members.Remove(member);
        NotifyPropertyChanged(nameof(MemberCountText));
        NotifyPropertyChanged(nameof(HasMembers));
        return true;
    }

    public async Task ResendMemberInviteAsync(FamilyMember member)
    {
        if (familyService == null || string.IsNullOrEmpty(member.Id)) return;
        try
        {
            await familyService.ResendInviteAsync(member.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] ResendMemberInviteAsync failed: {ex.Message}");
        }
    }

    public async Task ResendInviteAsync(PendingInvite? invite)
    {
        if (invite == null) return;
        if (familyService != null && !string.IsNullOrEmpty(invite.MemberId))
        {
            try
            {
                await familyService.ResendInviteAsync(invite.MemberId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] ResendInviteAsync failed: {ex.Message}");
            }
        }
        ResendInviteRequested?.Invoke(this, invite);
    }

    private async Task<IEnumerable<FamilyMember>?> TryGetCachedMembersAsync()
    {
        try { return await memberCache!.GetMembersAsync(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] Cache fetch failed: {ex.Message}");
            return null;
        }
    }

    private async Task<IEnumerable<FamilyMember>?> TryGetServiceMembersAsync()
    {
        try { return await familyService!.GetMembersAsync(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] Service fetch failed: {ex.Message}");
            return null;
        }
    }

    private void OpenRolePicker(FamilyMember? member)
    {
        if (member == null || member.IsYou) return;
        IsInviteSheetOpen = false;
        pickerMember = member;
        PickerMemberName = member.Name;
        RolePickerOptions.Clear();
        foreach (var template in rolePalette)
        {
            RolePickerOptions.Add(new RoleOption
            {
                Id = template.Id,
                Label = template.Label,
                Description = template.Description,
                TintColor = template.TintColor,
                ForegroundColor = template.ForegroundColor,
                IsCurrent = string.Equals(template.Id, member.Role, StringComparison.OrdinalIgnoreCase)
            });
        }
        IsRolePickerOpen = true;
    }

    private void CloseRolePicker()
    {
        IsRolePickerOpen = false;
        pickerMember = null;
    }

    private async Task PickRoleAsync(RoleOption? option)
    {
        if (option == null || pickerMember == null)
        {
            CloseRolePicker();
            return;
        }
        var member = pickerMember;
        if (familyService != null && !string.IsNullOrEmpty(member.Id))
        {
            try
            {
                var updated = await familyService.UpdateMemberRoleAsync(member.Id, option.Id);
                if (updated == null)
                {
                    CloseRolePicker();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] UpdateMemberRoleAsync failed: {ex.Message}");
                CloseRolePicker();
                return;
            }
        }
        memberCache?.Invalidate();
        ApplyRoleChange(member, option.Id);
        CloseRolePicker();
    }

    private void OpenInviteSheet()
    {
        IsRolePickerOpen = false;
        InviteEmail = string.Empty;
        InviteError = string.Empty;
        selectedInviteRole = null;
        InviteRoleOptions.Clear();
        // Default the new-invite selection to the second slot when available
        // (typically "Member"), or fall back to the first.
        var defaultIndex = rolePalette.Count > 1 ? 1 : 0;
        for (var i = 0; i < rolePalette.Count; i++)
        {
            var template = rolePalette[i];
            var isDefault = i == defaultIndex;
            var option = new RoleOption
            {
                Id = template.Id,
                Label = template.Label,
                Description = template.Description,
                TintColor = template.TintColor,
                ForegroundColor = template.ForegroundColor,
                IsCurrent = isDefault
            };
            if (isDefault) selectedInviteRole = option;
            InviteRoleOptions.Add(option);
        }
        IsInviteSheetOpen = true;
    }

    private void CloseInviteSheet()
    {
        IsInviteSheetOpen = false;
    }

    private void PickInviteRole(RoleOption? option)
    {
        if (option == null) return;
        foreach (var item in InviteRoleOptions)
            item.IsCurrent = ReferenceEquals(item, option);
        selectedInviteRole = option;
    }

    private async Task SendInviteAsync()
    {
        var email = InviteEmail?.Trim() ?? string.Empty;
        if (!IsValidEmail(email))
        {
            InviteError = "Enter a valid email address.";
            return;
        }

        var roleId = selectedInviteRole?.Id ?? "Co-parent";
        string memberId = string.Empty;
        if (familyService != null)
        {
            try
            {
                var created = await familyService.InviteMemberAsync(email, roleId);
                if (created == null)
                {
                    InviteError = "Could not send the invite. Please try again.";
                    return;
                }
                memberId = created.Id ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminRoleManagementViewModel] InviteMemberAsync failed: {ex.Message}");
                InviteError = "Could not send the invite. Please try again.";
                return;
            }
        }

        var invite = new PendingInvite
        {
            MemberId = memberId,
            Email = email,
            Role = selectedInviteRole?.Label ?? "Co-parent",
            InvitedAtRelative = "just now"
        };
        memberCache?.Invalidate();
        PendingInvites.Insert(0, invite);
        NotifyPropertyChanged(nameof(HasPendingInvites));
        IsInviteSheetOpen = false;
        InviteSentRequested?.Invoke(this, invite);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }

}
