namespace LotteryDetection.MultiTenancy.Payments.Paypal;

public class PayPalCaptureOrderRequestInput
{
    public PayPalCaptureOrderRequestInput(string orderId)
    {
        OrderId = orderId;
    }

    public string OrderId { get; set; }
}