namespace Jobsy.Core.Enums;

[Flags]
public enum TransportMode
{
    None = 0,
    Walking = 1,
    Bike = 2,
    Car = 4,
    PublicTransport = 8
}
