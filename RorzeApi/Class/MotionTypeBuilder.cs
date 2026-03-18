using System;

namespace RorzeApi.Class
{
    /// <summary>
    /// Motion Type 建構器 - 根據標準格式自動組成 Motion Type 字串
    /// 避免拼字錯誤，確保格式一致性
    /// </summary>
    public static class MotionTypeBuilder
    {
        #region 設備類型枚舉

        /// <summary>
        /// 設備位置類型
        /// </summary>
        public enum LocationType
        {
            LoadPort,
            Robot,
            Aligner,
            Chamber,
            Buffer,
            Equipment
        }

        /// <summary>
        /// Robot 手臂類型
        /// </summary>
        public enum ArmType
        {
            Arm_1 = 1,  // Lower Arm
            Arm_2 = 2   // Upper Arm
        }

        #endregion

        #region Transfer 動作

        /// <summary>
        /// 建構 Transfer 動作的 Motion Type
        /// 從來源位置轉移到目標位置
        /// </summary>
        /// <param name="sourceType">來源設備類型</param>
        /// <param name="sourceId">來源設備 ID</param>
        /// <param name="sourceSlot">來源 Slot (如果適用)</param>
        /// <param name="destType">目標設備類型</param>
        /// <param name="destId">目標設備 ID</param>
        /// <param name="destSlot">目標 Slot (如果適用)</param>
        /// <param name="armType">Robot 手臂類型 (如果是 Robot)</param>
        /// <returns>格式化的 Motion Type 字串</returns>
        public static string BuildTransfer(
            LocationType sourceType,
            int sourceId,
            int? sourceSlot,
            LocationType destType,
            int destId,
            int? destSlot,
            ArmType? armType = null)
        {
            string sourcePart = BuildLocationPart(sourceType, sourceId, sourceSlot);
            string destPart = BuildLocationPart(destType, destId, destSlot);
            string armPart = armType.HasValue ? $"_Arm_{(int)armType.Value}" : "";

            // 如果來源是 Robot，arm 資訊放在來源後面
            // 如果目標是 Robot，arm 資訊放在目標後面
            if (sourceType == LocationType.Robot)
            {
                return $"Transfer_{sourcePart}{armPart}_to_{destPart}";
            }
            else if (destType == LocationType.Robot)
            {
                return $"Transfer_{sourcePart}_to_{destPart}{armPart}";
            }
            else
            {
                return $"Transfer_{sourcePart}_to_{destPart}";
            }
        }

        /// <summary>
        /// Robot 從 LoadPort 取 Panel
        /// 範例：Transfer_LoadPort_2_Slot_6_to_Robot_1_Arm_1
        /// </summary>
        public static string RobotLoadFromLoadPort(
            int loadPortId,
            int slotId,
            int robotId,
            ArmType armType)
        {
            return BuildTransfer(
                LocationType.LoadPort, loadPortId, slotId,
                LocationType.Robot, robotId, null,
                armType
            );
        }

        /// <summary>
        /// Robot 放 Panel 到 LoadPort
        /// 範例：Transfer_Robot_1_Arm_1_to_LoadPort_2_Slot_6
        /// </summary>
        public static string RobotUnloadToLoadPort(
            int robotId,
            ArmType armType,
            int loadPortId,
            int slotId)
        {
            return BuildTransfer(
                LocationType.Robot, robotId, null,
                LocationType.LoadPort, loadPortId, slotId,
                armType
            );
        }

        /// <summary>
        /// Robot 放 Panel 到 Aligner
        /// 範例：Transfer_Robot_2_Arm_2_to_Aligner_1_Slot_1
        /// </summary>
        public static string RobotUnloadToAligner(
            int robotId,
            ArmType armType,
            int alignerId,
            int slotId = 1)
        {
            return BuildTransfer(
                LocationType.Robot, robotId, null,
                LocationType.Aligner, alignerId, slotId,
                armType
            );
        }

        /// <summary>
        /// Robot 從 Aligner 取 Panel
        /// 範例：Transfer_Aligner_1_Slot_1_to_Robot_2_Arm_2
        /// </summary>
        public static string RobotLoadFromAligner(
            int alignerId,
            int slotId,
            int robotId,
            ArmType armType)
        {
            return BuildTransfer(
                LocationType.Aligner, alignerId, slotId,
                LocationType.Robot, robotId, null,
                armType
            );
        }

        /// <summary>
        /// Robot 從 Chamber 取 Panel
        /// 範例：Transfer_Chamber_2_Slot_1_to_Robot_2_Arm_1
        /// </summary>
        public static string RobotLoadFromChamber(
            int chamberId,
            int slotId,
            int robotId,
            ArmType armType)
        {
            return BuildTransfer(
                LocationType.Chamber, chamberId, slotId,
                LocationType.Robot, robotId, null,
                armType
            );
        }

        /// <summary>
        /// Robot 放 Panel 到 Chamber
        /// 範例：Transfer_Robot_2_Arm_1_to_Chamber_2_Slot_1
        /// </summary>
        public static string RobotUnloadToChamber(
            int robotId,
            ArmType armType,
            int chamberId,
            int slotId)
        {
            return BuildTransfer(
                LocationType.Robot, robotId, null,
                LocationType.Chamber, chamberId, slotId,
                armType
            );
        }

        /// <summary>
        /// Robot 放 Panel 到 Equipment
        /// 範例：Transfer_Robot_1_Arm_2_to_Equipment_1_Slot_1
        /// </summary>
        public static string RobotUnloadToEquipment(
            int robotId,
            ArmType armType,
            int equipmentId,
            int slotId)
        {
            return BuildTransfer(
                LocationType.Robot, robotId, null,
                LocationType.Equipment, equipmentId, slotId,
                armType
            );
        }

        /// <summary>
        /// Robot 從 Equipment 取 Panel
        /// 範例：Transfer_Equipment_1_Slot_1_to_Robot_1_Arm_2
        /// </summary>
        public static string RobotLoadFromEquipment(
            int equipmentId,
            int slotId,
            int robotId,
            ArmType armType)
        {
            return BuildTransfer(
                LocationType.Equipment, equipmentId, slotId,
                LocationType.Robot, robotId, null,
                armType
            );
        }

        #endregion

        #region 其他動作類型

        /// <summary>
        /// LoadPort 開門動作
        /// 範例：DoorOpening_LoadPort_2
        /// </summary>
        public static string LoadPortDoorOpening(int loadPortId)
        {
            return $"DoorOpening_LoadPort_{loadPortId}";
        }

        /// <summary>
        /// LoadPort 關門動作
        /// 範例：DoorClosing_LoadPort_2
        /// </summary>
        public static string LoadPortDoorClosing(int loadPortId)
        {
            return $"DoorClosing_LoadPort_{loadPortId}";
        }

        /// <summary>
        /// LoadPort Clamp 動作
        /// 範例：Clamping_LoadPort_2
        /// </summary>
        public static string LoadPortClamping(int loadPortId)
        {
            return $"Clamping_LoadPort_{loadPortId}";
        }

        /// <summary>
        /// LoadPort Unclamp 動作
        /// 範例：Unclamping_LoadPort_2
        /// </summary>
        public static string LoadPortUnclamping(int loadPortId)
        {
            return $"Unclamping_LoadPort_{loadPortId}";
        }

        /// <summary>
        /// LoadPort Mapping 動作
        /// 範例：Mapping_LoadPort_2
        /// </summary>
        public static string LoadPortMapping(int loadPortId)
        {
            return $"Mapping_LoadPort_{loadPortId}";
        }

        /// <summary>
        /// Aligner 對位動作
        /// 範例：PanelAlignment_Aligner_1
        /// </summary>
        public static string AlignerAlignment(int alignerId)
        {
            return $"PanelAlignment_Aligner_{alignerId}";
        }

        /// <summary>
        /// Chamber 處理動作
        /// 範例：ChamberProcessing
        /// </summary>
        public static string ChamberProcessing()
        {
            return "ChamberProcessing";
        }

        /// <summary>
        /// Chamber 處理動作（指定 Chamber ID）
        /// 範例：ChamberProcessing_Chamber_1
        /// </summary>
        public static string ChamberProcessing(int chamberId)
        {
            return $"ChamberProcessing_Chamber_{chamberId}";
        }

        #endregion

        #region 私有輔助方法

        /// <summary>
        /// 建構位置部分的字串
        /// </summary>
        private static string BuildLocationPart(LocationType locationType, int id, int? slot)
        {
            string location = locationType.ToString();
            string slotPart = slot.HasValue ? $"_Slot_{slot.Value}" : "";
            return $"{location}_{id}{slotPart}";
        }

        #endregion

        #region 從枚舉轉換的輔助方法

        /// <summary>
        /// 從 enumRobotArms 轉換為 ArmType
        /// </summary>
        public static ArmType FromRobotArms(RorzeUnit.Class.Robot.Enum.enumRobotArms robotArm)
        {
            switch (robotArm)
            {
                case RorzeUnit.Class.Robot.Enum.enumRobotArms.LowerArm:
                    return ArmType.Arm_1;
                case RorzeUnit.Class.Robot.Enum.enumRobotArms.UpperArm:
                    return ArmType.Arm_2;
                case RorzeUnit.Class.Robot.Enum.enumRobotArms.BothArms:
                default:
                    return ArmType.Arm_1; // 預設使用 Arm_1
            }
        }

        #endregion
    }
}
