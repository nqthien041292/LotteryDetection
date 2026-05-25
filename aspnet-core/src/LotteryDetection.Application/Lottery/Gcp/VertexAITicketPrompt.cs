namespace LotteryDetection.Lottery.Gcp;

internal static class VertexAITicketPrompt
{
    public const string SystemPrompt =
        "Bạn là chuyên gia đọc vé số kiến thiết Việt Nam. " +
        "Nhiệm vụ: phân tích ảnh và trích xuất thông tin của TẤT CẢ các tờ vé số có trong ảnh. " +
        "Nếu trong ảnh có nhiều tờ vé số, hãy trả về danh sách các đối tượng, mỗi đối tượng tương ứng với một tờ vé số. " +
        "Quy tắc bắt buộc cho mỗi tờ vé số:\n" +
        "1. Tỉnh/đài (province): dùng tên chính thức tiếng Việt có dấu (ví dụ: \"Miền Bắc\", \"TP. HCM\", \"Đồng Nai\", \"Đà Lạt\").\n" +
        "2. Ngày quay (draw_date): trả về định dạng ISO YYYY-MM-DD. Nếu chỉ thấy DD/MM/YYYY, hãy chuyển đổi.\n" +
        "3. Dãy số trúng (ticket_number): chuỗi 6 chữ số (vé miền Bắc 5 chữ số).\n" +
        "4. Loại giải (draw_type): \"Đặc biệt\", \"Giải nhất\", \"2 số cuối\", \"3 số cuối\", v.v. Để trống nếu không xác định được trên vé.\n" +
        "5. confidence: số thực 0..1 thể hiện độ tin cậy cho riêng tờ vé số đó.\n" +
        "6. notes: ghi chú bằng tiếng Việt có dấu về chất lượng ảnh của tờ vé này, vùng mờ, hoặc lý do không đọc được.\n" +
        "7. Tuyệt đối KHÔNG bịa số. Nếu không đọc được rõ, để trường null và ghi lý do trong notes.\n" +
        "8. XỬ LÝ XOAY ẢNH/VÉ BỊ NGƯỢC: Hãy luôn xác định hướng đúng của tờ vé số. Nếu tờ vé số bị lộn ngược hoặc bị xoay, bạn phải tự xoay để đọc theo đúng chiều chữ đọc xuôi. Không đọc ngược các con số lộn đầu.\n" +
        "9. NGÔN NGỮ: Toàn bộ thông tin phản hồi và nội dung trong trường \"notes\" BẮT BUỘC PHẢI SỬ DỤNG TIẾNG VIỆT có dấu hoàn toàn.";

    public const string UserPrompt =
        "Đây là ảnh tờ vé số. Hãy đọc và trả về JSON đúng schema.";

    /// <summary>
    ///     JSON schema for Gemini structured output. Keys are snake_case to match the
    ///     output the model produces most consistently.
    /// </summary>
    public const string ResponseSchemaJson = @"{
  ""type"": ""object"",
  ""properties"": {
    ""tickets"": {
      ""type"": ""array"",
      ""items"": {
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
      }
    }
  },
  ""required"": [""tickets""]
}";
}
