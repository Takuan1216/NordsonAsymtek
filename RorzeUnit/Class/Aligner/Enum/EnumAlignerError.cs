using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.Aligner.Enum
{
    public enum enumAlignerError : int
    {
        Status_Error = 0,
        SendCommandFailure = 1,
        OriginPosReturnFailure = 2,
        OriginPosReturnTimeout = 3,
        MovingHomePosFailure = 4,
        AckTimeout = 5,
        ExtendingArmFailure = 6,
        TakeWaferFailure = 7,
        PutWaferFailure = 8,
        WaferExchangeFailure = 9,
        TurnOnVacuumFailure = 10,
        TurnOffVacuumFailure = 11,
        MappingFailure = 12,
        GetMappingDataFailure = 13,
        GetStatusFailure = 14,
        ResetFailure = 15,
        InitialFailure = 16,
        StopMotionTimeout = 17,
        StopMotionFailure = 18,
        PauseMotionTimeout = 19,
        PauseMotionFailure = 20,
        SetSpeedFailure = 21,
        SingleAxisAckTimeout = 22,
        SingleAxisOPRFailure = 23,
        SingleAxisAbsoluteMovingFailure = 24,
        SingleAxisJogMovingTimeout = 25,
        SingleAxisJogMovingFailure = 26,
        SingleAxisGetPosFailure = 27,
        MotionTimeout = 28,
        ModeTimeout = 29,
        EncoderBatteryError = 30,
        RobotNoInManualMode = 31,
        XAxisMotionTimeout = 32,
        NotFoundAvailableArm = 33,
        UploadTeachingDataFailure = 34,
        DownloadTeachingDataFailure = 35,
        ModeFailure = 36,
        RejectManualFuncWhenAuto = 37,
        GetMaintenanceDataFailure = 38,
        SetParameterFailure = 39,
        ProgramError = 40,
        RobotError = 41,
        RobotIsBusy = 42,
        ManualMode = 43,
        SettingWithoutWaferOut = 44,
        SettingWithoutWaferIn = 45,
        InterlockStop = 46,
        InitialRejectWhenAuto = 47,
        MotionAbnormal = 48,
        WaferAndDataNotMatch = 49,
        ClampTimeout,
        UnClampTimeout,
        //OCRReadFail=50

        //Agito controller error
        //AxisUnknow
        ControllerMovingTimeout = 0x1001,
        ControllerMovingError,
        ControllerOccurError,
        ControllerSetOutputError,
        ControllerHomingTimeout,
        ControllerHomingError,
        ControllerConnectError,
        ControllerIsMoving,
        InputAxisNoError,
        //AxisR Error
        AxisRMovingTimeout = 0x1101,
        AxisRMovingError,
        AxisRHomingTimeout,
        AxisRHomingError,


        //AxisY Error
        AxisYMovingTimeout = 0x1201,
        AxisYMovingError,
        AxisYHomingTimeout,
        AxisYHomingError,


        //AxisX Error
        AxisXMovingTimeout = 0x1301,
        AxisXMovingError,
        AxisXHomingTimeout,
        AxisXHomingError,

        //Camera Error
        CameraSocketError = 0x2001,//不能亂改數字 reference:SSCamera.GetAlignerErrorCode(string StateErrorCode)
        CameraTimeoutError,
        CameraInitialError,
        CameraImageIncompleteError,
        CameraFOUPTypeError,
        CameraParameterSettingFail,
        CameraAlgorithmCalculationFail,
        CameraGetParameterFail,
        CameraGrabImageError,
        CameraSaveTeachingDataError,
        CameraNotFoundWorksAndNotch,
        CameraNotFindFront,


        CameraCalibrateOffsetOutrange = 0x20A0,
        CameraAckLengthZero,
        CameraAckParameterLengthError,
        CameraCustomError,
        CameraCancelError,


        CameraDisconnect = 0x20C0,//不能亂改數字 reference:SSCamera.GetAlignerErrorCode(enumCustomError ErrorCode)
        CameraStatusError,
        CameraSendCommandFailure,
        CameraSendCommandAckTimeout,
        CameraInitialTimeout,
        CameraInitialFailure,
        CameraOriginTimeout,
        CameraOriginFailure,
        CameraMotionTimeout,
        CameraMotionAbnormal,
    }

    public enum enumAlignerWarning:int
    {
       

    }

    /*
    public enum enumAlignerAixsError : int
    {
        Others = 0x00,
        Xaxis = 0x01,
        Yaxis = 0x02,
        Zaxis = 0x03,
        Raxis = 0x04,
        Camera = 0x05,
    }
    public enum enumAlignerErrorCode : int
    {
        MotorStall = 0x01,
        Limit = 0x02,
        PositionErr = 0x03,
        CommandErr = 0x04,
        CommunicationErr = 0x05,
        SensorAbnormal = 0x06,
        DriverEMSErr = 0x07,
        WorkDroppedErr = 0x08,
        DriverErr = 0x0E,
        DriverPowerErr = 0x0F,
        ControlPowerErr = 0x10,
        DriverTempErr = 0x13,
        DriverFPGAErr = 0x14,
        MotorBroken = 0x15,
        MotorOverLoad = 0x16,
        MotorMotionErr = 0x17,
        AlignSensorErr = 0x18,
        AlignFanErr = 0x19,
        DriverIniternalErr = 0x40,
        ControlIniternalErr = 0x41,
        TaskIniternalErr = 0x42,
        ReadDataFail = 0x45,
        ChuckErr = 0x84,
        NotchDetectErr = 0x90,
        AlignSenDetectErr = 0x91,
        RetryOver = 0x92,
        IDReadErr = 0x93,
    }
    */

}
