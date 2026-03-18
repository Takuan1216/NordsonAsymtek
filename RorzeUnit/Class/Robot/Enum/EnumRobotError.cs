using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.Robot.Enum
{
    public enum enumRobotErrorCode : int
    {
        MotorStall = 1,
        SensorError = 2,
        EmergenyStop = 3,
        CommmandError = 4,
        CommunicationError = 5,
        waferCheckSensorError = 6,
        waferFall = 7,
        NotDefind = 8,
        OrgnNotComplet = 12,
        DriverError = 14,
        Unitoverheat = 18,
        FanError = 20,
        PosistionError = 21,
        EnocoderDatalost = 22,
        EnocoderCommunicationError = 23,
        Enocoderoverspeed = 24,
        EnocodEEPROMError = 25,
        EnocoderABSINCError = 26,
        EnocoderBlockError = 27,
        EnocoderTempError = 28,
        EnocoderResetError = 29,
        ControlPowerError = 32,
        DriverPowerError = 33,
        EEPROMEError = 34,
        overheat = 36,
        OverCurrent = 37,
        CableError = 38,

        ORGNDdisable = 131,
        WaferRetainingError = 132,
        InterlockSingleError = 133,
        AligmentError = 134,
        ExhaustFanError = 137,
        BatteryLow = 138,
        ClampError = 146,
        WaferPresenceError = 147
    }
    public enum enumRobotError : int
    {
        Status_Error = 2000,
        SendCommandFailure = 2001,
        OriginPosReturnFailure = 2002,
        OriginPosReturnTimeout = 2003,
        MovingHomePosFailure = 2004,
        AckTimeout = 2005,
        ExtendingArmFailure = 2006,
        TakeWaferFailure = 2007,
        PutWaferFailure = 2008,
        WaferExchangeFailure = 2009,
        TurnOnVacuumFailure = 2010,
        TurnOffVacuumFailure = 2011,
        MappingFailure = 2012,
        GetMappingDataFailure = 2013,
        GetStatusFailure = 2014,
        ResetFailure = 2015,
        InitialFailure = 2016,
        StopMotionTimeout = 2017,
        StopMotionFailure = 2018,
        PauseMotionTimeout = 2019,
        PauseMotionFailure = 2020,
        SetSpeedFailure = 2021,
        SingleAxisAckTimeout = 2022,
        SingleAxisOPRFailure = 2023,
        SingleAxisAbsoluteMovingFailure = 2024,
        SingleAxisJogMovingTimeout = 2025,
        SingleAxisJogMovingFailure = 2026,
        SingleAxisGetPosFailure = 2027,
        MotionTimeout = 2028,
        ModeTimeout = 2029,
        EncoderBatteryError = 2030,
        RobotNoInManualMode = 2031,
        XAxisMotionTimeout = 2032,
        NotFoundAvailableArm = 2033,
        UploadTeachingDataFailure = 2034,
        DownloadTeachingDataFailure = 2035,
        ModeFailure = 2036,
        RejectManualFuncWhenAuto = 2037,
        GetMaintenanceDataFailure = 2038,
        SetParameterFailure = 2039,
        ProgramError = 2040,
        RobotError = 2041,
        RobotIsBusy = 2042,
        ManualMode = 2043,
        SettingWithoutWaferOut = 2044,
        SettingWithoutWaferIn = 2045,
        InterlockStop = 2046,
        InitialRejectWhenAuto = 2047,
        MotionAbnormal = 2048,
        WaferAndDataNotMatch = 2049,
        ProcessFlagTimeout = 2050,
        ProcessFlagAbnormal = 2051,
        UnldDetectWafer = 2052,
        XaxisINP_NotDetect = 2053,
        BufferTriggerAroundSensor = 2054,
        RobotArmIsNotSafety = 2055,
        Robot_Pin_Not_Safety = 2056,

        //Stocker
        Stocker_RobotCheck_SensorErr = 2057,
        Stocker_Status_Error = 2058,
        Stocker_Is_Moving = 2059,
        Stocker_Opener_Detect_Frame_Drop_Down = 2060,
        Stocker_Opener_NO_Zaxis_down_sensor = 2061,
        Stocker_RbLoad_without_wafer = 2062,
        Stocker_RbUnld_wafer_exist = 2063,

        //Nordson Asymtek Misalign
        Panel_Misalign = 2064,




        Max = 2065,
    }


}
