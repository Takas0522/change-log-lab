namespace SreAgentLab.Models;

public static class TodoStatus
{
    public const string NotStarted = "未着手";
    public const string InProgress = "着手中";
    public const string Completed = "完了";

    public static readonly string[] All = [NotStarted, InProgress, Completed];

    public static bool IsValid(string status) => All.Contains(status);
}
