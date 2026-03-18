using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Class.Agito.Enum
{
    public enum enumAGD301Axis : int { AXS1 = 0, AXS2, AXS3, ALL };

    //enumAGD301Axis m_eXax = enumAGD301Axis.AXS1;
    public enum enumMotionTimeout : int { ORGN = 0, MREL, MABS, STEP, STOP }
    public enum enumInPosStat : int { ServoOff = 0, ServoOn, Moving, Stabilizing, InPos }
    public enum enumHomeStat : int {HomeOff = 0, HomingComplete = 100 }


    public enum enumAgitoError : int
    {
        ControllerConnectError = 0x1010,
        InitialFailure,
        InputAxisNoError,

        Axis123HomingError = 0x1100,
        Axis1HomingError,
        Axis2HomingError,
        Axis3HomingError,
        Axis123HomingTimeout,
        Axis1HomingTimeout,
        Axis2HomingTimeout,
        Axis3HomingTimeout,
        Axis123MovingError,
        Axis1MovingError,
        Axis2MovingError,
        Axis3MovingError,
        Axis123MovingTimeout,
        Axis1MovingTimeout,
        Axis2MovingTimeout,
        Axis3MovingTimeout,
        Axis123IsMoving,
        Axis1IsMoving,
        Axis2IsMoving,
        Axis3IsMoving,
        Axis123MovingControllerError,
        Axis1MovingControllerError,
        Axis2MovingControllerError,
        Axis3MovingControllerError,
        Axis1AutoPhaseError,
        Axis2AutoPhaseError,
        Axis3AutoPhaseError,
    }



    }
