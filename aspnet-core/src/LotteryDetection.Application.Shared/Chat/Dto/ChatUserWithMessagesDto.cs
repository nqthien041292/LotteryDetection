using System.Collections.Generic;

namespace LotteryDetection.Chat.Dto;

public class ChatUserWithMessagesDto : ChatUserDto
{
    public List<ChatMessageDto> Messages { get; set; }
}

