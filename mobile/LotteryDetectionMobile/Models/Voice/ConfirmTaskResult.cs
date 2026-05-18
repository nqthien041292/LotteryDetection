namespace LotteryDetectionMobile.Models.Voice;

/// <summary>
///     Result of confirming a voice task for creation in Microsoft 365.
/// </summary>
public class ConfirmTaskResult
{
    /// <summary>
    ///     Voice task ID.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    ///     Current status after confirmation.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    ///     User-friendly message about the task creation.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}