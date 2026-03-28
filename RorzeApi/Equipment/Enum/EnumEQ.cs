using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Class.EQ.Enum
{
    public enum enumControlState
    {
        OFFLINE = 0,
        EQOFFLINE = 1,
        ATTEMPTONLINE = 2,
        HOSTOFFLINE = 3,
        ONLINELOCAL = 4,
        ONLINEREMOTE = 5
    }

    public enum enumMachineStatus
    {
        IDLE = 0,
        ACTION = 1,
        ALARM = 2,
    }

    //待確認指令
    public enum enumSendCmd
    {
        Unknow,
        Hello,
        RecipeList,
        PrepareToReceiveWafer,
        PutWaferFinish,
        ProcessWafer,
        GetWaferFinish,
        Stop,
        Retry,
        Abort,
        Status,
        UnloadWafer,
        Alarm,
        SoftwareVersion,
        MachineType,
        ModeLock,
        ProcessFinish,
        EQToSafePosition,
        Result,
    }


    public enum enumEQError //  自定義的異常
    {
        AckTimeout = 1,
        AckAbnormal = 2,
        MotionTimeout = 3,
        MotionAbnormal = 4,
        EvenTimeout = 5,
        EvenAbnormal = 6,
        InterlockStop = 7,
        OccursError = 8,//收到異常




        ReadFileDeleteException = 9,

        IOException = 10,


        initial_fail = 11,
        load_recipe_fail = 12,

        Socket_Disconnected = 13,

        Unknown_Command = 14,
        ShutterDoorCloseFail = 15,
        ShutterDoor1_protrude_sensor_detect = 16, //KN add 0328
        ShutterDoor2_protrude_sensor_detect = 17, //KN add 0328
        ShutterDoor3_protrude_sensor_detect = 18, //KN add 0328
        ShutterDoor4_protrude_sensor_detect = 19, //KN add 0328


    }










}
