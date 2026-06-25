namespace OrderClientApp.Domain.Orders;

public enum OrderStatus
{
    Unprocessed = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Processing = 4,
    WaitingForArrival = 5,
    PartiallyReceived = 6,
    Completed = 7,
    Canceled = 8
}

public static class OrderStatusExtensions
{
    public static string ToJapaneseLabel(this OrderStatus status)
        => status switch
        {
            OrderStatus.Unprocessed => "未処理",
            OrderStatus.PendingApproval => "申請中",
            OrderStatus.Approved => "承認済み",
            OrderStatus.Rejected => "却下",
            OrderStatus.Processing => "処理中",
            OrderStatus.WaitingForArrival => "入荷待ち",
            OrderStatus.PartiallyReceived => "部分入荷",
            OrderStatus.Completed => "完了",
            OrderStatus.Canceled => "キャンセル",
            _ => "不明"
        };
}
