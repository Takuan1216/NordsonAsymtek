using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Class.Keyence_MP.Enum
{

    public enum enumKeyence_MPCommand : int
    {
        None = -1,
        AirPressure,
        AirFlow,
        AirTemperature,
        InputVoltage,
        InputCurrent,
        CumulativeAirConsumption,
        CumulativePowerConsumption,
    };
    public enum enumKeyence_MPError : int
    {
        Status_Error = 0,
        SendCommandFailure = 1,
        AckTimeout = 2,
    }
}
