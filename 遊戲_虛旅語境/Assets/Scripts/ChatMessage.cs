using System;

[Serializable]
public class ChatMessage
{
    public string role;   // "player" 或 "ai"
    public string name;   // 玩家或 AI 名字
    public string text;   // 對話內容
    public string time;   // 可選
}