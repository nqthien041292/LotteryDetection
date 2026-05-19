namespace LotteryDetection.Lottery.Gcp;

internal static class VertexAITicketPrompt
{
    public const string SystemPrompt =
        "Bạn là chuyên gia đọc vé số kiến thiết Việt Nam. " +
        "Nhiệm vụ: phân tích ảnh tờ vé số và trích xuất thông tin chính xác. " +
        "Quy tắc bắt buộc:\n" +
        "1. Tỉnh/đài (province): dùng tên chính thức tiếng Việt có dấu (ví dụ: \"Miền Bắc\", \"TP. Hồ Chí Minh\", \"Đồng Nai\", \"Vĩnh Long\").\n" +
        "2. Ngày quay (draw_date): trả về định dạng ISO YYYY-MM-DD. Nếu chỉ thấy DD/MM/YYYY, hãy chuyển đổi.\n" +
        "3. Dãy số trúng (ticket_number): chuỗi 6 chữ số (vé miền Bắc 5 chữ số). Nếu ảnh có nhiều dãy, nối bằng dấu phẩy.\n" +
        "4. Loại giải (draw_type): \"Đặc biệt\", \"Giải nhất\", \"2 số cuối\", \"3 số cuối\", v.v. Để trống nếu không xác định được trên vé.\n" +
        "5. confidence: số thực 0..1 thể hiện độ tin cậy tổng thể.\n" +
        "6. notes: ghi chú về chất lượng ảnh, vùng mờ, hoặc trường không đọc được.\n" +
        "7. Tuyệt đối KHÔNG bịa số. Nếu không đọc được rõ, để trường null và ghi lý do trong notes.";

    public const string UserPrompt =
        "Đây là ảnh tờ vé số. Hãy đọc và trả về JSON đúng schema.";

    /// <summary>
    ///     JSON schema for Gemini structured output. Keys are snake_case to match the
    ///     output the model produces most consistently.
    /// </summary>
    public const string ResponseSchemaJson = @"{
  ""type"": ""object"",
  ""properties"": {
    ""province"":      { ""type"": ""string"", ""nullable"": true },
    ""draw_date"":     { ""type"": ""string"", ""nullable"": true, ""description"": ""YYYY-MM-DD"" },
    ""ticket_number"": { ""type"": ""string"", ""nullable"": true },
    ""draw_type"":     { ""type"": ""string"", ""nullable"": true },
    ""confidence"":    { ""type"": ""number"" },
    ""notes"":         { ""type"": ""string"", ""nullable"": true }
  },
  ""required"": [""confidence""]
}";
}
