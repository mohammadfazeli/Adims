enum OrderState
{
    None,           // قبل از شروع
    PaymentPending,
    PaymentCompleted,
    InventoryPending,
    InventoryReserved,
    ShippingPending,
    Shipped,
    Completed,
    Failed,
    Compensating
}

enum OrderTrigger
{
    StartOrder,             // ورودی اولیه (مثلاً OrderCreated)
    PaymentSucceeded,
    PaymentFailed,
    InventoryReserved,
    InventoryFailed,
    ShippingSucceeded,
    ShippingFailed,
    CompensationCompleted,
    Timeout
}
