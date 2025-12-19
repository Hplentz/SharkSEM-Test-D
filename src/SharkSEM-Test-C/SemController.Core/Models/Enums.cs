namespace SemController.Core.Models;

public enum VacuumStatus
{
    Error = -1,
    Ready = 0,
    Pumping = 1,
    Venting = 2,
    VacuumOff = 3,
    ChamberOpen = 4
}

public enum BeamState
{
    Unknown = -1,
    Off = 0,
    On = 1,
    Transitioning = 1000
}

public enum BlankerMode
{
    Off = 0,
    On = 1,
    Auto = 2
}

public enum VacuumGauge
{
    Chamber = 0,
    TmpGauge = 1,
    SemGun = 2,
    FibColumn = 3,
    FibGun = 4,
    SemColumn = 5,
    XeValve = 6
}

public enum VacuumMode
{
    Unknown = -1,
    HighVacuum = 0,
    VariablePressure = 1
}

public enum SemType
{
    Tescan,
    Mock,
    Custom
}
