using System;
using System.Collections.Generic;
using Castle.Components.DictionaryAdapter;
using LotteryDetection.Friendships.Dto;

namespace LotteryDetection.Chat.Dto;

public class GetUserChatFriendsWithSettingsOutput
{
    public GetUserChatFriendsWithSettingsOutput()
    {
        Friends = new EditableList<FriendDto>();
    }

    public DateTime ServerTime { get; set; }

    public List<FriendDto> Friends { get; set; }
}