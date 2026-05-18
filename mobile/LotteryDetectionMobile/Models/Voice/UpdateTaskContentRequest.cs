namespace LotteryDetectionMobile.Models.Voice;

/// <summary>
///     Request body for updating a voice task's content fields.
///     Only non-null fields will be applied (partial update).
/// </summary>
public class UpdateTaskContentRequest
{
    public string Title { get; set; }
    public string Assignee { get; set; }
    public string DueDateTime { get; set; }
    public string Priority { get; set; }
    public string Location { get; set; }
    public string Category { get; set; }
    public string Notes { get; set; }
}