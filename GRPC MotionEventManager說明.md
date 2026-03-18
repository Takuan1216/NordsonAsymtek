# MotionEventManager 使用說明

## 📋 目錄
1. [概述](#概述)
2. [主要類別介紹](#主要類別介紹)
3. [快速開始](#快速開始)
4. [使用場景](#使用場景)
5. [API 參考](#api-參考)
6. [配置說明](#配置說明)
7. [常見問題](#常見問題)

---

## 概述

`MotionEventManager` 是一個用於管理設備動作事件的單例管理器，它能夠：
- 自動追蹤設備的 Motion Start/End 事件
- 通過 GRPC/HTTP 將動作事件發送到遠端伺服器
- 支援多設備並發管理
- 提供靈活的配置選項
- 內建超時處理和錯誤恢復機制

### 主要特性

✅ **事件驅動架構** - 透過訂閱事件自動觸發
✅ **單例模式** - 全局統一管理
✅ **線程安全** - 支援並發操作
✅ **自動記錄** - 詳細的日誌記錄
✅ **超時控制** - 可配置的 HTTP 超時
✅ **錯誤容忍** - 超時或錯誤時可選擇繼續執行

---

## 主要類別介紹

### 1. MotionEventManager (事件管理器)

**職責：** 統一管理所有設備的 Motion 事件

**主要方法：**
- `SetBaseUrl(string)` - 設定 API 伺服器位址
- `TriggerMotionStart(...)` - 手動觸發 Motion Start
- `TriggerMotionEnd(...)` - 手動觸發 Motion End
- `SetHttpTimeout(int)` - 設定 HTTP 超時時間
- `SetContinueOnError(bool)` - 設定錯誤處理策略

### 2. GRPC (HTTP 客戶端)

**職責：** 負責實際的 HTTP 通信

**主要方法：**
- `SendMotionStart(...)` - 發送 Motion Start 到伺服器
- `SendMotionEnd(...)` - 發送 Motion End 到伺服器
- `UpdateConfig(...)` - 更新配置參數

### 3. MotionConfig (配置類)

**職責：** 儲存 Motion 事件的配置參數

**主要屬性：**
- `HttpTimeoutSeconds` - HTTP 請求超時時間（秒）
- `ContinueOnError` - 發生錯誤時是否繼續執行
- `RecipeName` - Recipe 名稱
- `FoupId` - Foup ID
- `Timeout` - Motion 超時時間

---

## 快速開始

### Step 1: 初始化 MotionEventManager

```csharp
// 在 frmMDI.cs 的建構函數中
var motionManager = MotionEventManager.Instance;

// 設定 API 伺服器位址
motionManager.SetBaseUrl("http://localhost:61723");

// 啟用日誌記錄
motionManager.EnableLogging(true);

// 設定 HTTP 超時為 10 秒
motionManager.SetHttpTimeout(10);

// 設定發生錯誤時繼續執行（不中斷程式）
motionManager.SetContinueOnError(true);
```

### Step 2: 註冊設備（Robot 範例）

```csharp
// 方法 1: 使用自動註冊（推薦）
foreach (I_Robot robot in ListTRB)
{
    if (robot.Disable) continue;

    motionManager.RegisterUnit(
        robot,
        "TRB",
        r => r.BodyNo,
        "OnLoadComplete"  // Motion End 事件名稱
    );
}

// 方法 2: 手動訂閱事件
robot.OnLoadComplete += (sender, e) =>
{
    motionManager.TriggerMotionEnd("TRB", robot.BodyNo, e);
};
```

### Step 3: 觸發 Motion Start

```csharp
// 在 Robot 開始動作前手動觸發
public void Load(int nStg0to399, enumRobotArms eArm, int nSlot)
{
    // 建構 Motion Type 字串
    var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
        nStg0to399,          // 位置編號
        this.BodyNo,         // Robot ID
        MotionEventManager.MotionType.FromRobotArms(eArm),  // 手臂類型
        nSlot,               // Slot 編號
        isLoad: true         // true = 取料
    );

    // 觸發 Motion Start
    MotionEventManager.Instance.TriggerMotionStart("ROBOT", this.BodyNo, motionType);

    // 執行實際動作...
    DoLoad(nStg0to399, eArm, nSlot);
}
```

---

## 使用場景

### 場景 1: Robot 從 LoadPort 取 Panel

```csharp
// 在 SSRobotRR75x.cs 的 Load 方法中
public void Load(int nStg0to399, enumRobotArms eArm, int nSlot)
{
    try
    {
        // 1. 建構 Motion Type
        var armType = MotionEventManager.MotionType.FromRobotArms(eArm);
        var motionType = MotionEventManager.MotionType.RobotLoadFromLoadPort(
            loadPortId: 1,     // LoadPort 1
            slotId: nSlot,     // Slot 編號
            robotId: this.BodyNo,  // Robot ID
            armType: armType   // 手臂類型
        );
        // 結果：Transfer_LoadPort_1_Slot_6_to_Robot_1_Arm_1

        // 2. 觸發 Motion Start
        MotionEventManager.Instance.TriggerMotionStart("TRB", this.BodyNo, motionType);

        // 3. 執行實際動作
        DoLoad(nStg0to399, eArm, nSlot);

        // 4. Motion End 會在 OnLoadComplete 事件中自動觸發
    }
    catch (Exception ex)
    {
        _logger.WriteLog($"Load failed: {ex.Message}");
    }
}
```

**日誌輸出：**
```
2026/01/02 10:30:00.100    [MotionEventManager] [MotionStart] TRB#1 - Transfer_LoadPort_1_Slot_6_to_Robot_1_Arm_1
2026/01/02 10:30:00.123    ==> Start TRB#1 motion 'Transfer_LoadPort_1_Slot_6_to_Robot_1_Arm_1'
2026/01/02 10:30:00.456    Start response: {"status":"ok"}
2026/01/02 10:30:05.789    [MotionEventManager] [MotionEnd] TRB#1 - Duration: 5.67s
```

### 場景 2: Robot 放 Panel 到 Aligner

```csharp
public void UnloadToAligner(int alignerId, enumRobotArms eArm, int slotId)
{
    // 建構 Motion Type
    var armType = MotionEventManager.MotionType.FromRobotArms(eArm);
    var motionType = MotionEventManager.MotionType.RobotUnloadToAligner(
        robotId: this.BodyNo,
        armType: armType,
        alignerId: alignerId,
        slotId: slotId
    );
    // 結果：Transfer_Robot_1_Arm_2_to_Aligner_1_Slot_1

    // 觸發並執行
    MotionEventManager.Instance.TriggerMotionStart("TRB", this.BodyNo, motionType);
    DoUnload(alignerId, eArm, slotId);
}
```

### 場景 3: LoadPort Mapping

```csharp
// 在 SSLoadPort 類中
public void Mapping()
{
    // 建構 Motion Type
    var motionType = MotionEventManager.MotionType.LoadPortMapping(this.BodyNo);
    // 結果：Mapping_LoadPort_2

    // 觸發 Motion Start
    MotionEventManager.Instance.TriggerMotionStart("STG", this.BodyNo, motionType);

    // 執行 Mapping
    ExecuteMapping();

    // 手動觸發 Motion End
    MotionEventManager.Instance.TriggerMotionEnd("STG", this.BodyNo);
}
```

### 場景 4: Aligner 對位

```csharp
// 在 SSAligner 類中
public void Align(double targetAngle)
{
    var motionType = MotionEventManager.MotionType.AlignerAlignment(this.BodyNo);
    // 結果：PanelAlignment_Aligner_1

    MotionEventManager.Instance.TriggerMotionStart("ALN", this.BodyNo, motionType);
    DoAlignment(targetAngle);
    MotionEventManager.Instance.TriggerMotionEnd("ALN", this.BodyNo);
}
```

### 場景 5: 使用位置編號自動建構 Motion Type

```csharp
// 當你只知道 Robot 的 Stage 位置編號時
public void LoadFromPosition(int nStg0to399, enumRobotArms eArm, int nSlot)
{
    // 自動根據位置判斷設備類型
    var armType = MotionEventManager.MotionType.FromRobotArms(eArm);
    var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
        position: nStg0to399,     // 例如：30 = LoadPort 2
        robotId: this.BodyNo,
        armType: armType,
        slotId: nSlot,
        isLoad: true
    );
    // 位置 30 會自動識別為 LoadPort 2
    // 結果：Transfer_LoadPort_2_Slot_5_to_Robot_1_Arm_1

    MotionEventManager.Instance.TriggerMotionStart("TRB", this.BodyNo, motionType);
    DoLoad(nStg0to399, eArm, nSlot);
}
```

---

## API 參考

### MotionEventManager 類

#### 配置方法

```csharp
// 設定 API 基礎 URL
void SetBaseUrl(string baseUrl)

// 啟用/停用日誌
void EnableLogging(bool enable)

// 設定全局 HTTP 超時時間（秒）
void SetHttpTimeout(int timeoutSeconds)

// 設定特定設備的 HTTP 超時
void SetHttpTimeout(string unitType, int unitId, int timeoutSeconds)

// 設定全局錯誤處理策略
void SetContinueOnError(bool continueOnError)

// 設定特定設備的錯誤處理
void SetContinueOnError(string unitType, int unitId, bool continueOnError)

// 更新特定設備的配置
void UpdateConfig(string unitType, int unitId,
    string recipeName = null,
    string foupId = null,
    string panelId = null,
    string lotId = null,
    string part6 = null)
```

#### 事件觸發方法

```csharp
// 手動觸發 Motion Start
void TriggerMotionStart(string unitType, int unitId, string motionType, object sourceData = null)

// 手動觸發 Motion End
void TriggerMotionEnd(string unitType, int unitId, object sourceData = null)

// 註冊設備並自動綁定事件
void RegisterUnit<TUnit>(
    TUnit unit,
    string unitType,
    Func<TUnit, int> getUnitId,
    string eventNameForEnd)
```

#### 查詢方法

```csharp
// 檢查設備是否有正在進行的動作
bool IsMotionActive(string unitType, int unitId)

// 取得目前動作資訊
string GetCurrentMotion(string unitType, int unitId)
```

### MotionType 建構器類

```csharp
// Robot Transfer 相關
string RobotLoadFromLoadPort(int loadPortId, int slotId, int robotId, ArmType armType)
string RobotUnloadToLoadPort(int robotId, ArmType armType, int loadPortId, int slotId)
string RobotLoadFromAligner(int alignerId, int slotId, int robotId, ArmType armType)
string RobotUnloadToAligner(int robotId, ArmType armType, int alignerId, int slotId = 1)
string RobotLoadFromChamber(int chamberId, int slotId, int robotId, ArmType armType)
string RobotUnloadToChamber(int robotId, ArmType armType, int chamberId, int slotId)
string RobotLoadFromEquipment(int equipmentId, int slotId, int robotId, ArmType armType)
string RobotUnloadToEquipment(int robotId, ArmType armType, int equipmentId, int slotId)

// LoadPort 相關
string LoadPortDoorOpening(int loadPortId)
string LoadPortDoorClosing(int loadPortId)
string LoadPortClamping(int loadPortId)
string LoadPortUnclamping(int loadPortId)
string LoadPortMapping(int loadPortId)

// Aligner 相關
string AlignerAlignment(int alignerId)

// Chamber 相關
string ChamberProcessing()
string ChamberProcessing(int chamberId)

// 自動建構（根據位置編號）
string BuildTransferFromPosition(
    int position,           // Stage 位置編號 (0-399)
    int robotId,           // Robot ID
    ArmType armType,       // 手臂類型
    int slotId,            // Slot 編號
    bool isLoad)           // true = 取料, false = 放料
```

---

## 配置說明

### MotionConfig 屬性說明

| 屬性 | 類型 | 預設值 | 說明 |
|-----|------|--------|------|
| `ToolId` | string | "" | 工具 ID（會自動設為 {unitType}_Tool） |
| `MachineName` | string | "" | 機台名稱（會自動設為 {unitType}_Station） |
| `SoftwareVersion` | string | 自動取得 | 軟體版本號 |
| `EffmNumber` | string | "" | EFEM 編號（會自動設為 {unitType}_{unitId}） |
| `RecipeName` | string | "DemoRecipe" | Recipe 名稱 |
| `FoupId` | string | "FOUP123" | Foup ID |
| `PanelId` | string | "PANEL123" | Panel ID |
| `LotId` | string | "LOT123" | Lot ID |
| `Part6` | string | "PART6" | Part6 參數 |
| `Timeout` | int | 30 | Motion 超時時間（秒） |
| `HttpTimeoutSeconds` | int | 5 | HTTP 請求超時（秒） |
| `ContinueOnError` | bool | true | 錯誤時是否繼續執行 |

### 設備類型（unitType）定義

| unitType | 說明 | 範例 |
|----------|------|------|
| `TRB` | Robot | TRB#1, TRB#2 |
| `STG` | LoadPort | STG#1 ~ STG#8 |
| `ALN` | Aligner | ALN#1, ALN#2 |
| `BUF` | Buffer | BUF#1, BUF#2 |
| `EQM` | Equipment | EQM#1 ~ EQM#4 |

### 位置編號對應表

| 位置範圍 | 設備類型 | 設備 ID |
|---------|---------|---------|
| 1-4 | Equipment | EQM 1-4 |
| 10-29 | LoadPort | STG 1 |
| 30-49 | LoadPort | STG 2 |
| 50-69 | LoadPort | STG 3 |
| 70-89 | LoadPort | STG 4 |
| 90-109 | LoadPort | STG 5 |
| 110-129 | LoadPort | STG 6 |
| 130-149 | LoadPort | STG 7 |
| 150-169 | LoadPort | STG 8 |
| 179 | Buffer | BUF 1 |
| 180 | Buffer | BUF 2 |
| 182 | Aligner | ALN 1 |
| 183 | Aligner | ALN 2 |

---

## 常見問題

### Q1: 如何設定不同設備的超時時間？

```csharp
var motionManager = MotionEventManager.Instance;

// Robot 設定較短超時（3 秒）
motionManager.SetHttpTimeout("TRB", 1, 3);
motionManager.SetHttpTimeout("TRB", 2, 3);

// LoadPort 設定較長超時（10 秒）
motionManager.SetHttpTimeout("STG", 1, 10);
motionManager.SetHttpTimeout("STG", 2, 10);
```

### Q2: 超時或錯誤時如何處理？

```csharp
// 方式 1: 全局設定 - 發生錯誤時繼續執行（不拋出異常）
motionManager.SetContinueOnError(true);  // 預設值

// 方式 2: 全局設定 - 發生錯誤時拋出異常
motionManager.SetContinueOnError(false);

// 方式 3: 針對關鍵設備單獨設定
motionManager.SetContinueOnError("TRB", 1, false);  // Robot 1 錯誤要處理
motionManager.SetContinueOnError("STG", 3, true);   // LoadPort 3 可以忽略
```

### Q3: 如何查看超時錯誤的日誌？

日誌會自動記錄到 `Log\YYYYMMDD\GRPC.log`：

```
2026/01/02 10:30:55.123    [TIMEOUT] HTTP request timeout after 5s: http://localhost:61723/api/motion/start
2026/01/02 10:30:55.456    [ERROR] HTTP request failed: http://localhost:61723/api/motion/end - Connection refused
```

### Q4: Motion Type 字串格式是什麼？

標準格式：
- Robot 取料：`Transfer_{Source}_{SourceId}_Slot_{Slot}_to_Robot_{RobotId}_Arm_{Arm}`
- Robot 放料：`Transfer_Robot_{RobotId}_Arm_{Arm}_to_{Dest}_{DestId}_Slot_{Slot}`
- LoadPort 動作：`{Action}_LoadPort_{Id}`
- Aligner 動作：`PanelAlignment_Aligner_{Id}`

範例：
```
Transfer_LoadPort_2_Slot_6_to_Robot_1_Arm_1
Transfer_Robot_1_Arm_2_to_Aligner_1_Slot_1
Mapping_LoadPort_3
PanelAlignment_Aligner_1
```

### Q5: 如何手動更新配置參數？

```csharp
// 方式 1: 更新特定參數
motionManager.UpdateConfig("TRB", 1,
    recipeName: "NewRecipe",
    foupId: "FOUP456",
    lotId: "LOT789"
);

// 方式 2: 使用 MotionConfig 物件
var config = new MotionConfig
{
    RecipeName = "Recipe_A",
    FoupId = "FOUP_001",
    PanelId = "PANEL_001",
    LotId = "LOT_001",
    HttpTimeoutSeconds = 15,
    ContinueOnError = true
};
motionManager.UpdateConfig("TRB", 1, config);
```

### Q6: 伺服器無法連線時會發生什麼？

當 `ContinueOnError = true`（預設）時：
1. 記錄錯誤日誌
2. 返回錯誤 JSON 字串
3. **程式繼續執行**，不會中斷

當 `ContinueOnError = false` 時：
1. 記錄錯誤日誌
2. **拋出異常**
3. 程式中斷，需要 try-catch 處理

### Q7: 如何檢查設備目前的動作狀態？

```csharp
// 檢查是否有正在進行的動作
bool isActive = motionManager.IsMotionActive("TRB", 1);

// 取得目前動作類型
string currentMotion = motionManager.GetCurrentMotion("TRB", 1);
if (currentMotion != null)
{
    Console.WriteLine($"Robot 1 正在執行: {currentMotion}");
}
```

---

## 完整範例

### 範例 1: Robot 完整動作流程

```csharp
public class SSRobotRR75x : I_Robot
{
    private MotionEventManager _motionManager = MotionEventManager.Instance;

    // 從 LoadPort 取 Panel
    public void LoadFromLoadPort(int loadPortId, int slotId, enumRobotArms arm)
    {
        try
        {
            // 建構 Motion Type
            var armType = MotionEventManager.MotionType.FromRobotArms(arm);
            var motionType = MotionEventManager.MotionType.RobotLoadFromLoadPort(
                loadPortId, slotId, this.BodyNo, armType
            );

            // 觸發 Motion Start
            _motionManager.TriggerMotionStart("TRB", this.BodyNo, motionType);

            // 執行實際動作
            int stageIndex = CalculateStageIndex(loadPortId);
            DoLoad(stageIndex, arm, slotId);

            // Motion End 會在 OnLoadComplete 事件中自動觸發
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"LoadFromLoadPort failed: {ex.Message}");
        }
    }

    // 放 Panel 到 Aligner
    public void UnloadToAligner(int alignerId, int slotId, enumRobotArms arm)
    {
        var armType = MotionEventManager.MotionType.FromRobotArms(arm);
        var motionType = MotionEventManager.MotionType.RobotUnloadToAligner(
            this.BodyNo, armType, alignerId, slotId
        );

        _motionManager.TriggerMotionStart("TRB", this.BodyNo, motionType);

        int stageIndex = GetAlignerStageIndex(alignerId);
        DoUnload(stageIndex, arm, slotId);
    }
}
```

### 範例 2: 初始化設定（frmMDI.cs）

```csharp
public frmMDI()
{
    // ... 其他初始化代碼 ...

    // 初始化 MotionEventManager
    var motionManager = MotionEventManager.Instance;
    motionManager.SetBaseUrl(GParam.theInst.GetMotionEventManagerUrl());
    motionManager.EnableLogging(true);

    // 設定全局超時 10 秒
    motionManager.SetHttpTimeout(10);

    // 設定錯誤時繼續執行
    motionManager.SetContinueOnError(true);

    // 註冊所有 Robot
    foreach (I_Robot robot in ListTRB)
    {
        if (robot.Disable) continue;

        // 自動註冊 OnLoadComplete 和 OnUnldComplete 事件
        motionManager.RegisterUnit(robot, "TRB", r => r.BodyNo, "OnLoadComplete");
        motionManager.RegisterUnit(robot, "TRB", r => r.BodyNo, "OnUnldComplete");
    }

    // 註冊所有 LoadPort
    foreach (I_Loadport loadport in ListSTG)
    {
        if (loadport.Disable) continue;
        motionManager.RegisterUnit(loadport, "STG", lp => lp.BodyNo, "OnMappingComplete");
    }
}
```

---

## 總結

`MotionEventManager` 提供了一個簡單但強大的方式來管理設備動作事件。透過：

1. **統一的事件管理** - 所有設備使用相同的介面
2. **自動化的事件追蹤** - 減少手動編碼
3. **靈活的配置選項** - 適應不同的使用場景
4. **完善的錯誤處理** - 提高系統穩定性
5. **詳細的日誌記錄** - 方便問題追蹤和除錯

您可以快速整合到現有系統中，提升系統的可觀測性和可維護性。

---

**文件版本:** 1.0
**最後更新:** 2026/01/02
**聯絡資訊:** 請參考專案 README
