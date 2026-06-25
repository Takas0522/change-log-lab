namespace OrderClientApp.Domain.Orders;

public enum OrderStatus
{
    Unprocessed = 0,
    Processing = 1,
    WaitingForArrival = 2,
    PartiallyReceived = 3,
    Completed = 4,
    Canceled = 5
}

public static class OrderStatusExtensions
{
    public static string ToJapaneseLabel(this OrderStatus status)
        => status switch
        {
            OrderStatus.Unprocessed => "未処理",
            OrderStatus.Processing => "処理中",
            OrderStatus.WaitingForArrival => "入荷待ち",
            OrderStatus.PartiallyReceived => "部分入荷",
            OrderStatus.Completed => "完了",
            OrderStatus.Canceled => "キャンセル",
            _ => "不明"
        };
}
