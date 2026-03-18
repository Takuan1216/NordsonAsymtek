using RorzeComm.Log;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RorzeApi.Class
{
    #region Event 參數定義

    /// <summary>
    /// Motion 開始事件參數
    /// </summary>
    public class MotionStartEventArgs : EventArgs
    {
        public string UnitType { get; set; }
        public int UnitId { get; set; }
        public string MotionType { get; set; }
        public object SourceData { get; set; }  // 可攜帶原始資料

        /// <summary>
        /// 建構 Motion 開始事件參數
        /// </summary>
        /// <param name="unitType">單體類型</param>
        /// <param name="unitId">單體編號</param>
        /// <param name="motionType">動作類型</param>
        /// <param name="sourceData">原始資料（可選）</param>
        public MotionStartEventArgs(string unitType, int unitId, string motionType, object sourceData = null)
        {
            UnitType = unitType;
            UnitId = unitId;
            MotionType = motionType;
            SourceData = sourceData;
        }
    }

    /// <summary>
    /// Motion 結束事件參數
    /// </summary>
    public class MotionEndEventArgs : EventArgs
    {
        public string UnitType { get; set; }
        public int UnitId { get; set; }
        public object SourceData { get; set; }

        /// <summary>
        /// 建構 Motion 結束事件參數
        /// </summary>
        /// <param name="unitType">單體類型</param>
        /// <param name="unitId">單體編號</param>
        /// <param name="sourceData">原始資料（可選）</param>
        public MotionEndEventArgs(string unitType, int unitId, object sourceData = null)
        {
            UnitType = unitType;
            UnitId = unitId;
            SourceData = sourceData;
        }
    }

    #endregion

    /// <summary>
    /// GRPC Motion 事件管理器 - 只需要在外部註冊事件即可
    /// </summary>
    public class MotionEventManager
    {
        #region Singleton

        private static readonly Lazy<MotionEventManager> _instance =
            new Lazy<MotionEventManager>(() => new MotionEventManager());

        public static MotionEventManager Instance => _instance.Value;

        private MotionEventManager() { }

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<string, GRPC> _grpcClients = new ConcurrentDictionary<string, GRPC>();
        private readonly ConcurrentDictionary<string, MotionContext> _activeMotions = new ConcurrentDictionary<string, MotionContext>();
        private string _defaultBaseUrl = "http://localhost:61723";
        private bool _enableLogging = true;
        private static SLogger _logger = SLogger.GetLogger("GRPC");

        /// <summary>
        /// Motion 上下文資訊（用於追蹤進行中的動作）
        /// </summary>
        private class MotionContext
        {
            public string UnitType { get; set; }
            public int UnitId { get; set; }
            public string Motion { get; set; }
            public DateTime StartTime { get; set; }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// 設定 API 基礎 URL
        /// </summary>
        public void SetBaseUrl(string baseUrl)
        {
            _defaultBaseUrl = baseUrl;
        }

        /// <summary>
        /// 啟用/停用日誌
        /// </summary>
        public void EnableLogging(bool enable)
        {
            _enableLogging = enable;
        }

        /// <summary>
        /// 更新指定單體的配置
        /// </summary>
        public void UpdateConfig(string unitType, int unitId,
            string recipeName = null, string foupId = null,
            string panelId = null, string lotId = null, string part6 = null)
        {
            string key = GetKey(unitType, unitId);
            var client = GetOrCreateClient(key);
            client.UpdateConfig(recipeName, foupId, panelId, lotId, part6);
        }

        /// <summary>
        /// 批次更新配置
        /// </summary>
        public void UpdateConfig(string unitType, int unitId, MotionConfig config)
        {
            string key = GetKey(unitType, unitId);
            var client = GetOrCreateClient(key);
            client.UpdateConfig(c =>
            {
                c.RecipeName = config.RecipeName;
                c.FoupId = config.FoupId;
                c.PanelId = config.PanelId;
                c.LotId = config.LotId;
                c.Part6 = config.Part6;
                c.HttpTimeoutSeconds = config.HttpTimeoutSeconds;
                c.ContinueOnError = config.ContinueOnError;
            });
        }

        /// <summary>
        /// 從 SWafer 物件更新配置
        /// </summary>
        /// <param name="unitType">單體類型</param>
        /// <param name="unitId">單體編號</param>
        /// <param name="wafer">SWafer 物件</param>
        public void UpdateConfigFromWafer(string unitType, int unitId, RorzeUnit.Class.SWafer wafer)
        {
            if (wafer == null) return;

            string key = GetKey(unitType, unitId);
            var client = GetOrCreateClient(key);
            client.UpdateConfig(
                recipeName: wafer.RecipeID,
                foupId: wafer.FoupID,
                panelId: wafer.WaferInforID_B,  // 使用 WaferID_F 背刻 作為 PanelId
                lotId: wafer.LotID,
                part6: null  // Part6 保持預設值
            );

            if (_enableLogging)
                Log($"[UpdateFromWafer] {unitType}#{unitId} - Recipe={wafer.RecipeID}, Foup={wafer.FoupID}, Panel={wafer.WaferID_F}, Lot={wafer.LotID}");
        }

        /// <summary>
        /// 設定 HTTP 超時時間（秒）
        /// </summary>
        public void SetHttpTimeout(int timeoutSeconds)
        {
            foreach (var client in _grpcClients.Values)
            {
                client.UpdateConfig(c => c.HttpTimeoutSeconds = timeoutSeconds);
            }
        }

        /// <summary>
        /// 設定特定單體的 HTTP 超時時間
        /// </summary>
        public void SetHttpTimeout(string unitType, int unitId, int timeoutSeconds)
        {
            string key = GetKey(unitType, unitId);
            var client = GetOrCreateClient(key);
            client.UpdateConfig(c => c.HttpTimeoutSeconds = timeoutSeconds);
        }

        /// <summary>
        /// 設定發生錯誤時是否繼續執行
        /// </summary>
        public void SetContinueOnError(bool continueOnError)
        {
            foreach (var client in _grpcClients.Values)
            {
                client.UpdateConfig(c => c.ContinueOnError = continueOnError);
            }
        }

        /// <summary>
        /// 設定特定單體發生錯誤時是否繼續執行
        /// </summary>
        public void SetContinueOnError(string unitType, int unitId, bool continueOnError)
        {
            string key = GetKey(unitType, unitId);
            var client = GetOrCreateClient(key);
            client.UpdateConfig(c => c.ContinueOnError = continueOnError);
        }

        #endregion

        #region Event Handlers (核心方法)

        /// <summary>
        /// Motion Start 事件處理器（供外部訂閱）
        /// </summary>
        public async void OnMotionStart(object sender, MotionStartEventArgs e)
        {
            // 檢查是否停用 GRPC 功能
            if (GParam.theInst.GetGRPC_Disable() || GParam.theInst.IsSimulate)
                return;

            try
            {
                string key = GetKey(e.UnitType, e.UnitId);
                var client = GetOrCreateClient(key);

                // 發送 Motion Start
                await client.SendMotionStart(e.UnitType, e.UnitId, e.MotionType);

                // 記錄上下文
                _activeMotions[key] = new MotionContext
                {
                    UnitType = e.UnitType,
                    UnitId = e.UnitId,
                    Motion = e.MotionType,
                    StartTime = DateTime.Now
                };

                if (_enableLogging)
                    Log($"[MotionStart] {e.UnitType}#{e.UnitId} - {e.MotionType}");
            }
            catch (Exception ex)
            {
                LogError($"OnMotionStart failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Motion End 事件處理器（供外部訂閱）
        /// </summary>
        public async void OnMotionEnd(object sender, MotionEndEventArgs e)
        {
            // 檢查是否停用 GRPC 功能
            if (GParam.theInst.GetGRPC_Disable() || GParam.theInst.IsSimulate)
                return;

            try
            {
                string key = GetKey(e.UnitType, e.UnitId);
                var client = GetOrCreateClient(key);

                // 發送 Motion End
                await client.SendMotionEnd(e.UnitType, e.UnitId);

                // 清理上下文
                if (_activeMotions.TryRemove(key, out var context))
                {
                    var duration = DateTime.Now - context.StartTime;
                    if (_enableLogging)
                        Log($"[MotionEnd] {e.UnitType}#{e.UnitId} - Duration: {duration.TotalSeconds:F2}s");
                }
            }
            catch (Exception ex)
            {
                LogError($"OnMotionEnd failed: {ex.Message}");
            }
        }

        #endregion

        #region 便捷的適配器方法

        /// <summary>
        /// 為現有的 LoadUnldEventArgs 建立適配器
        /// 適用於 Robot 的 OnLoadComplete 事件
        /// </summary>
        public EventHandler<T> CreateMotionEndAdapter<T>(string unitType, int unitId) where T : EventArgs
        {
            return (sender, e) =>
            {
                var endArgs = new MotionEndEventArgs(unitType, unitId, e);
                OnMotionEnd(sender, endArgs);
            };
        }

        /// <summary>
        /// 建立 Motion Start 觸發器
        /// 用於在動作開始前手動觸發
        /// </summary>
        public void TriggerMotionStart(string unitType, int unitId, string motionType, object sourceData = null)
        {
            var startArgs = new MotionStartEventArgs(unitType, unitId, motionType, sourceData);
            OnMotionStart(this, startArgs);
        }

        /// <summary>
        /// 建立 Motion End 觸發器
        /// 用於在動作結束後手動觸發
        /// </summary>
        public void TriggerMotionEnd(string unitType, int unitId, object sourceData = null)
        {
            var endArgs = new MotionEndEventArgs(unitType, unitId, sourceData);
            OnMotionEnd(this, endArgs);
        }

        /// <summary>
        /// 建立 Motion Start 觸發器
        /// 用於在動作開始前手動觸發
        /// Config 根據sWafer內容設定
        /// </summary>
        public void TriggerMotionStartWithWafer(string unitType, int unitId, string motionType, RorzeUnit.Class.SWafer wafer)
        {
            // 先從 SWafer 更新配置
            UpdateConfigFromWafer(unitType, unitId, wafer);

            // 再觸發 Motion Start
            TriggerMotionStart(unitType, unitId, motionType, wafer);
        }
        #endregion

        #region 單體註冊方法

        /// <summary>
        /// 註冊單體，自動綁定現有事件
        /// </summary>
        public void RegisterUnit<TUnit>(
            TUnit unit,
            string unitType,
            Func<TUnit, int> getUnitId,
            string eventNameForEnd) where TUnit : class
        {
            int unitId = getUnitId(unit);

            // 使用反射查找並訂閱結束事件
            var endEvent = typeof(TUnit).GetEvent(eventNameForEnd);
            if (endEvent != null)
            {
                var adapter = CreateMotionEndAdapter<EventArgs>(unitType, unitId);
                var delegateType = endEvent.EventHandlerType;
                var handlerDelegate = Delegate.CreateDelegate(delegateType, adapter.Target, adapter.Method);
                endEvent.AddEventHandler(unit, handlerDelegate);

                if (_enableLogging)
                    Log($"[Register] {unitType}#{unitId} - Event '{eventNameForEnd}' bound");
            }
            else
            {
                LogError($"Event '{eventNameForEnd}' not found on type {typeof(TUnit).Name}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 根據單體類型和編號生成唯一鍵值
        /// </summary>
        /// <param name="unitType">單體類型</param>
        /// <param name="unitId">單體編號</param>
        /// <returns>格式為 "{unitType}_{unitId}" 的鍵值字串</returns>
        private string GetKey(string unitType, int unitId)
        {
            return $"{unitType}_{unitId}";
        }

        /// <summary>
        /// 取得或建立 GRPC 客戶端實例
        /// </summary>
        /// <param name="key">單體的唯一鍵值</param>
        /// <returns>對應的 GRPC 客戶端實例</returns>
        private GRPC GetOrCreateClient(string key)
        {
            return _grpcClients.GetOrAdd(key, k => new GRPC(_defaultBaseUrl));
        }

        /// <summary>
        /// 記錄一般日誌訊息
        /// </summary>
        /// <param name="message">要記錄的訊息</param>
        private void Log(string message)
        {
            if (_enableLogging)
                _logger.WriteLog($"[MotionEventManager] {message}");
        }

        /// <summary>
        /// 記錄錯誤日誌訊息
        /// </summary>
        /// <param name="message">要記錄的錯誤訊息</param>
        private void LogError(string message)
        {
            _logger.WriteLog($"[MotionEventManager ERROR] {message}");
        }

        /// <summary>
        /// 檢查是否有正在進行的動作
        /// </summary>
        public bool IsMotionActive(string unitType, int unitId)
        {
            string key = GetKey(unitType, unitId);
            return _activeMotions.ContainsKey(key);
        }

        /// <summary>
        /// 取得目前動作資訊
        /// </summary>
        public string GetCurrentMotion(string unitType, int unitId)
        {
            string key = GetKey(unitType, unitId);
            if (_activeMotions.TryGetValue(key, out var context))
            {
                return context.Motion;
            }
            return null;
        }

        /// <summary>
        /// 清理所有客戶端
        /// </summary>
        public void Cleanup()
        {
            _grpcClients.Clear();
            _activeMotions.Clear();
        }

        #endregion

        #region Motion Type 建構器

        /// <summary>
        /// Motion Type 建構器 - 根據標準格式自動組成 Motion Type 字串
        /// 避免拼字錯誤，確保格式一致性
        /// 使用方式：MotionEventManager.MotionType.RobotLoadFromLoadPort(...)
        /// </summary>
        public static class MotionType
        {
            #region 單體類型枚舉

            /// <summary>
            /// 單體位置類型
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

            #region Robot Transfer 動作

            /// <summary>
            /// 建構 Transfer 動作的 Motion Type
            /// </summary>
            public static string BuildTransfer(
                LocationType sourceType, int sourceId, int? sourceSlot,
                LocationType destType, int destId, int? destSlot,
                ArmType? armType = null)
            {
                string sourcePart = BuildLocationPart(sourceType, sourceId, sourceSlot);
                string destPart = BuildLocationPart(destType, destId, destSlot);
                string armPart = armType.HasValue ? $"_Arm_{(int)armType.Value}" : "";

                if (sourceType == LocationType.Robot)
                    return $"Transfer_{sourcePart}{armPart}_to_{destPart}";
                else if (destType == LocationType.Robot)
                    return $"Transfer_{sourcePart}_to_{destPart}{armPart}";
                else
                    return $"Transfer_{sourcePart}_to_{destPart}";
            }

            /// <summary>
            /// Robot 從 LoadPort 取 Panel
            /// 範例：Transfer_LoadPort_2_Slot_6_to_Robot_1_Arm_1
            /// </summary>
            public static string RobotLoadFromLoadPort(int loadPortId, int slotId, int robotId, ArmType armType)
            {
                return BuildTransfer(LocationType.LoadPort, loadPortId, slotId, LocationType.Robot, robotId, null, armType);
            }

            /// <summary>
            /// Robot 放 Panel 到 LoadPort
            /// 範例：Transfer_Robot_1_Arm_1_to_LoadPort_2_Slot_6
            /// </summary>
            public static string RobotUnloadToLoadPort(int robotId, ArmType armType, int loadPortId, int slotId)
            {
                return BuildTransfer(LocationType.Robot, robotId, null, LocationType.LoadPort, loadPortId, slotId, armType);
            }

            /// <summary>
            /// Robot 放 Panel 到 Aligner
            /// 範例：Transfer_Robot_2_Arm_2_to_Aligner_1_Slot_1
            /// </summary>
            public static string RobotUnloadToAligner(int robotId, ArmType armType, int alignerId, int slotId = 1)
            {
                return BuildTransfer(LocationType.Robot, robotId, null, LocationType.Aligner, alignerId, slotId, armType);
            }

            /// <summary>
            /// Robot 從 Aligner 取 Panel
            /// 範例：Transfer_Aligner_1_Slot_1_to_Robot_2_Arm_2
            /// </summary>
            public static string RobotLoadFromAligner(int alignerId, int slotId, int robotId, ArmType armType)
            {
                return BuildTransfer(LocationType.Aligner, alignerId, slotId, LocationType.Robot, robotId, null, armType);
            }

            /// <summary>
            /// Robot 從 Chamber 取 Panel
            /// 範例：Transfer_Chamber_2_Slot_1_to_Robot_2_Arm_1
            /// </summary>
            public static string RobotLoadFromChamber(int chamberId, int slotId, int robotId, ArmType armType)
            {
                return BuildTransfer(LocationType.Chamber, chamberId, slotId, LocationType.Robot, robotId, null, armType);
            }

            /// <summary>
            /// Robot 放 Panel 到 Chamber
            /// 範例：Transfer_Robot_2_Arm_1_to_Chamber_2_Slot_1
            /// </summary>
            public static string RobotUnloadToChamber(int robotId, ArmType armType, int chamberId, int slotId)
            {
                return BuildTransfer(LocationType.Robot, robotId, null, LocationType.Chamber, chamberId, slotId, armType);
            }

            /// <summary>
            /// Robot 放 Panel 到 Equipment
            /// 範例：Transfer_Robot_1_Arm_2_to_Equipment_1_Slot_1
            /// </summary>
            public static string RobotUnloadToEquipment(int robotId, ArmType armType, int equipmentId, int slotId)
            {
                return BuildTransfer(LocationType.Robot, robotId, null, LocationType.Equipment, equipmentId, slotId, armType);
            }

            /// <summary>
            /// Robot 從 Equipment 取 Panel
            /// 範例：Transfer_Equipment_1_Slot_1_to_Robot_1_Arm_2
            /// </summary>
            public static string RobotLoadFromEquipment(int equipmentId, int slotId, int robotId, ArmType armType)
            {
                return BuildTransfer(LocationType.Equipment, equipmentId, slotId, LocationType.Robot, robotId, null, armType);
            }

            #endregion

            #region 其他動作類型( Aligner Chamber )

            /// <summary>
            /// LoadPort 開門動作 - 範例：DoorOpening_LoadPort_2
            /// </summary>
            public static string LoadPortDoorOpening(int loadPortId) => $"DoorOpening_LoadPort_{loadPortId}";

            /// <summary>
            /// LoadPort 關門動作 - 範例：DoorClosing_LoadPort_2
            /// </summary>
            public static string LoadPortDoorClosing(int loadPortId) => $"DoorClosing_LoadPort_{loadPortId}";

            /// <summary>
            /// Aligner 對位動作 - 範例：PanelAlignment_Aligner_1
            /// </summary>
            public static string AlignerAlignment(int alignerId) => $"PanelAlignment_Aligner_{alignerId}";

            /// <summary>
            /// Chamber 處理動作 - 範例：ChamberProcessing
            /// </summary>
            public static string ChamberProcessing() => "ChamberProcessing";

            /// <summary>
            /// Chamber 處理動作（指定 Chamber ID） - 範例：ChamberProcessing_Chamber_1
            /// </summary>
            public static string ChamberProcessing(int chamberId) => $"ChamberProcessing_Chamber_{chamberId}";

            #endregion

            #region 輔助方法

            /// <summary>
            /// 建立位置字串部分（用於組合 Motion Type）
            /// </summary>
            /// <param name="locationType">位置類型</param>
            /// <param name="id">位置編號</param>
            /// <param name="slot">Slot 編號（可選）</param>
            /// <returns>格式化的位置字串，例如："LoadPort_2_Slot_6"</returns>
            private static string BuildLocationPart(LocationType locationType, int id, int? slot)
            {
                string location = locationType.ToString();
                string slotPart = slot.HasValue ? $"_Slot_{slot.Value}" : "";
                return $"{location}_{id}{slotPart}";
            }

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
                        return ArmType.Arm_1;
                }
            }

            /// <summary>
            /// 根據位置編號自動建構 Motion Type
            /// 從 GlobalParameters.enumRbtAddress 位置對應到正確的單體類型
            /// </summary>
            /// <param name="position">位置編號 (nStg0to399)</param>
            /// <param name="robotId">Robot ID</param>
            /// <param name="armType">Robot 手臂類型</param>
            /// <param name="slotId">Slot 編號</param>
            /// <param name="isLoad">true = Robot 從位置取料, false = Robot 放料到位置</param>
            /// <returns>格式化的 Motion Type 字串</returns>
            public static string BuildTransferFromPosition(
                int position,
                int robotId,
                ArmType armType,
                int slotId,
                bool isLoad)
            {
                LocationType locationType;
                int locationId;

                // 根據位置範圍判斷單體類型和 ID
                if (position >= 1 && position <= 4)
                {
                    // EQM1-4: Equipment
                    locationType = LocationType.Equipment;
                    locationId = position;
                }
                else if (position >= 10 && position <= 29)
                {
                    // STG1: LoadPort 1 (10-29)
                    locationType = LocationType.LoadPort;
                    locationId = 1;
                }
                else if (position >= 30 && position <= 49)
                {
                    // STG2: LoadPort 2 (30-49)
                    locationType = LocationType.LoadPort;
                    locationId = 2;
                }
                else if (position >= 50 && position <= 69)
                {
                    // STG3: LoadPort 3 (50-69)
                    locationType = LocationType.LoadPort;
                    locationId = 3;
                }
                else if (position >= 70 && position <= 89)
                {
                    // STG4: LoadPort 4 (70-89)
                    locationType = LocationType.LoadPort;
                    locationId = 4;
                }
                else if (position >= 90 && position <= 109)
                {
                    // STG5: LoadPort 5 (90-109)
                    locationType = LocationType.LoadPort;
                    locationId = 5;
                }
                else if (position >= 110 && position <= 129)
                {
                    // STG6: LoadPort 6 (110-129)
                    locationType = LocationType.LoadPort;
                    locationId = 6;
                }
                else if (position >= 130 && position <= 149)
                {
                    // STG7: LoadPort 7 (130-149)
                    locationType = LocationType.LoadPort;
                    locationId = 7;
                }
                else if (position >= 150 && position <= 169)
                {
                    // STG8: LoadPort 8 (150-169)
                    locationType = LocationType.LoadPort;
                    locationId = 8;
                }
                else if (position == 179)
                {
                    // BUF1: Buffer 1
                    locationType = LocationType.Buffer;
                    locationId = 1;
                }
                else if (position == 180)
                {
                    // BUF2: Buffer 2
                    locationType = LocationType.Buffer;
                    locationId = 2;
                }
                else if (position == 182)
                {
                    // ALN1: Aligner 1
                    locationType = LocationType.Aligner;
                    locationId = 1;
                }
                else if (position == 183)
                {
                    // ALN2: Aligner 2
                    locationType = LocationType.Aligner;
                    locationId = 2;
                }
                else
                {
                    // 未知位置，預設為 Equipment
                    throw new ArgumentException($"無法識別的位置編號: {position}");
                }

                // 根據 isLoad 決定使用哪個建構方法
                if (isLoad)
                {
                    // Robot 從位置取料
                    switch (locationType)
                    {
                        case LocationType.LoadPort:
                            return RobotLoadFromLoadPort(locationId, slotId, robotId, armType);
                        case LocationType.Aligner:
                            return RobotLoadFromAligner(locationId, slotId, robotId, armType);
                        case LocationType.Chamber:
                            return RobotLoadFromChamber(locationId, slotId, robotId, armType);
                        case LocationType.Equipment:
                            return RobotLoadFromEquipment(locationId, slotId, robotId, armType);
                        default:
                            throw new NotSupportedException($"不支援從 {locationType} 類型取料");
                    }
                }
                else
                {
                    // Robot 放料到位置
                    switch (locationType)
                    {
                        case LocationType.LoadPort:
                            return RobotUnloadToLoadPort(robotId, armType, locationId, slotId);
                        case LocationType.Aligner:
                            return RobotUnloadToAligner(robotId, armType, locationId, slotId);
                        case LocationType.Chamber:
                            return RobotUnloadToChamber(robotId, armType, locationId, slotId);
                        case LocationType.Equipment:
                            return RobotUnloadToEquipment(robotId, armType, locationId, slotId);
                        default:
                            throw new NotSupportedException($"不支援放料到 {locationType} 類型");
                    }
                }
            }

            #endregion
        }

        #endregion
    }

    #region 擴展方法（可選）

    /// <summary>
    /// 為常見類型提供擴展方法
    /// </summary>
    public static class MotionEventManagerExtensions
    {
        /// <summary>
        /// 觸發 Motion Start（擴展方法）
        /// </summary>
        public static void RaiseMotionStart(this object sender, string unitType, int unitId, string motionType)
        {
            MotionEventManager.Instance.TriggerMotionStart(unitType, unitId, motionType, sender);
        }

        /// <summary>
        /// 觸發 Motion End（擴展方法）
        /// </summary>
        public static void RaiseMotionEnd(this object sender, string unitType, int unitId)
        {
            MotionEventManager.Instance.TriggerMotionEnd(unitType, unitId, sender);
        }
    }

    #endregion
}
