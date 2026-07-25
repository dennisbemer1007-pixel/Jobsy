namespace Jobsy.Core.Enums;

[Flags]
public enum WorkType
{
    None = 0,
    Horeca = 1,
    Winkel = 2,
    Logistiek = 4,
    Tuinbouw = 8,
    Zorg = 16,
    Kantoor = 32,
    Bouw = 64,
    Schoonmaak = 128,
    Productie = 256
}
