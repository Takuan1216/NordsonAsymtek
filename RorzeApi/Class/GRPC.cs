using RorzeComm.Log;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RorzeApi.Class
{
    /// <summary>
    /// Motion 請求的配置參數
    /// </summary>
    public class MotionConfig
    {
        public string ToolId { get; set; }
        public string MachineName { get; set; }
        public string SoftwareVersion { get; set; }
        public string EffmNumber { get; set; }
        public string RecipeName { get; set; }
        public string FoupId { get; set; }
        public string PanelId { get; set; }
        public string LotId { get; set; }
        public string Part6 { get; set; }
        public int Timeout { get; set; }
        public int HttpTimeoutSeconds { get; set; } // HTTP 請求超時時間(秒)
        public bool ContinueOnError { get; set; } // 發生錯誤時是否繼續執行

        /// <summary>
        /// 建構函數 - 初始化 Motion 配置的預設值
        /// </summary>
        public MotionConfig()
        {
            // 預設值 - 某些欄位會在發送時根據 unitType/unitId 動態設定
            ToolId = "";  // 將被設定為 {unitType}_Tool
            MachineName = "";  // 將被設定為 {unitType}_Station
            SoftwareVersion = GetAssemblyVersion(); // 軟體版本
            EffmNumber = "";  // 將被設定為 {unitType}_{unitId}
            RecipeName = "EMPTY";  // 預設 Recipe 名稱
            FoupId = "EMPTY";         // 預設 Foup ID
            PanelId = "EMPTY";       // 預設 Panel ID
            LotId = "EMPTY";           // 預設 Lot ID
            Part6 = "EMPTY";            // 預設 Part6
            Timeout = 30;
            HttpTimeoutSeconds = 5; // 預設 HTTP 超時 5 秒
            ContinueOnError = true; // 預設發生錯誤時繼續執行
        }

        /// <summary>
        /// 取得組件版本號碼
        /// </summary>
        /// <returns>格式化的版本字串（Major.Minor.Build），失敗時回傳 "1.0.0"</returns>
        private static string GetAssemblyVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                return "1.0.0";
            }
        }



        /// <summary>
        /// 複製配置物件（深層複製）
        /// </summary>
        /// <returns>新的 MotionConfig 實例，包含相同的配置值</returns>
        public MotionConfig Clone()
        {
            return new MotionConfig
            {
                ToolId = this.ToolId,
                MachineName = this.MachineName,
                SoftwareVersion = this.SoftwareVersion,
                EffmNumber = this.EffmNumber,
                RecipeName = this.RecipeName,
                FoupId = this.FoupId,
                PanelId = this.PanelId,
                LotId = this.LotId,
                Part6 = this.Part6,
                Timeout = this.Timeout,
                HttpTimeoutSeconds = this.HttpTimeoutSeconds,
                ContinueOnError = this.ContinueOnError
            };
        }
    }

    /// <summary>
    /// Motion 事件參數（用於 GRPC 類別的事件回報）
    /// </summary>
    public class MotionEventArgs : EventArgs
    {
        public string UnitType { get; set; }
        public int UnitId { get; set; }
        public string Motion { get; set; }
        public string Response { get; set; }
        public bool IsStart { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 基於 HTTP 的 Motion API 客戶端，用於發送 Motion 開始/結束事件
    /// </summary>
    public class GRPC
    {
        private static SLogger _logger = SLogger.GetLogger("GRPC_Client");

        #region Static Members

        private static readonly JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
        private static MotionConfig defaultConfig = new MotionConfig();

        /// <summary>
        /// 建立具有指定超時時間的 HttpClient
        /// </summary>
        /// <param name="timeoutSeconds">超時時間（秒）</param>
        /// <returns>配置好超時時間的 HttpClient 實例</returns>
        private static HttpClient CreateHttpClient(int timeoutSeconds)
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
        }

        /// <summary>
        /// 設定靜態方法使用的預設 Motion 配置
        /// </summary>
        /// <param name="config">要設定的 MotionConfig 配置物件</param>
        public static void SetDefaultConfig(MotionConfig config)
        {
            if (config != null)
            {
                defaultConfig = config;
            }
        }

        /// <summary>
        /// 取得目前靜態方法使用的預設配置
        /// </summary>
        /// <returns>目前的預設 MotionConfig 配置物件</returns>
        public static MotionConfig GetDefaultConfig()
        {
            return defaultConfig;
        }

        #endregion

        #region Instance Members 

        private readonly string baseUrl;
        private MotionConfig instanceConfig;

        /// <summary>
        /// 當發送 Motion Start/End 時觸發的事件
        /// </summary>
        public event EventHandler<MotionEventArgs> MotionEventSent;

        /// <summary>
        /// 建構函數 - 用於建立 GRPC 實例
        /// </summary>
        /// <param name="baseUrl">API 基礎 URL（例如：http://localhost:61723）</param>
        /// <param name="config">可選的 Motion 配置參數</param>
        public GRPC(string baseUrl, MotionConfig config = null)
        {
            this.baseUrl = baseUrl;
            this.instanceConfig = config ?? new MotionConfig();
        }

        /// <summary>
        /// 更新實例配置（使用 Action 委託）
        /// </summary>
        /// <param name="updateAction">更新配置的委託函數</param>
        public void UpdateConfig(Action<MotionConfig> updateAction)
        {
            if (updateAction != null)
            {
                updateAction(instanceConfig);
            }
        }

        /// <summary>
        /// 更新特定的配置值
        /// </summary>
        /// <param name="recipeName">Recipe 名稱</param>
        /// <param name="foupId">Foup ID</param>
        /// <param name="panelId">Panel ID</param>
        /// <param name="lotId">Lot ID</param>
        /// <param name="part6">Part6 參數</param>
        public void UpdateConfig(string recipeName = null, string foupId = null,
            string panelId = null, string lotId = null, string part6 = null)
        {
            if (recipeName != null) instanceConfig.RecipeName = recipeName;
            if (foupId != null) instanceConfig.FoupId = foupId;
            if (panelId != null) instanceConfig.PanelId = panelId;
            if (lotId != null) instanceConfig.LotId = lotId;
            if (part6 != null) instanceConfig.Part6 = part6;
        }

        /// <summary>
        /// 取得目前實例的配置
        /// </summary>
        /// <returns>目前的 MotionConfig 配置物件</returns>
        public MotionConfig GetConfig()
        {
            return instanceConfig;
        }

        #endregion

        #region Common Helper Methods

        /// <summary>
        /// 取得目前時間戳記（Unix time milliseconds 格式）
        /// </summary>
        /// <returns>Unix time milliseconds 格式的時間戳記（long）</returns>
        private static long GetCurrentTimestamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        /// <summary>
        /// 發送 HTTP POST 請求，傳送 JSON 格式的資料
        /// </summary>
        /// <param name="url">目標 URL</param>
        /// <param name="payload">要傳送的資料物件（將被序列化為 JSON）</param>
        /// <param name="timeoutSeconds">HTTP 請求超時時間（秒），預設 5 秒</param>
        /// <param name="continueOnError">發生錯誤時是否繼續執行，預設 true</param>
        /// <returns>伺服器回應的字串（成功時為回應內容，失敗時為錯誤 JSON）</returns>
        private static async Task<string> HttpPostJson(string url, object payload, int timeoutSeconds = 5, bool continueOnError = true)
        {
            var json = jsonSerializer.Serialize(payload);
            _logger.WriteLog($"[JSON Payload] {json}");
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using (var httpClient = CreateHttpClient(timeoutSeconds))
                {
                    _logger.WriteLog($"[HTTP POST] URL: {url}");
                    var response = await httpClient.PostAsync(url, content);
                    _logger.WriteLog($"[HTTP Response] StatusCode: {response.StatusCode} ({(int)response.StatusCode})");
                    _logger.WriteLog($"[HTTP Response] IsSuccessStatusCode: {response.IsSuccessStatusCode}");

                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.WriteLog($"[HTTP Response] Body Length: {responseBody?.Length ?? 0}");
                    _logger.WriteLog($"[HTTP Response] Body: {(string.IsNullOrEmpty(responseBody) ? "(empty)" : responseBody)}");

                    return responseBody;
                }
            }
            catch (TaskCanceledException ex)
            {
                // 超時錯誤
                string errorMsg = $"HTTP request timeout after {timeoutSeconds}s: {url}";
                _logger.WriteLog($"[TIMEOUT] {errorMsg}");

                if (continueOnError)
                {
                    return $"{{\"error\": \"timeout\", \"message\": \"{errorMsg}\"}}";
                }
                else
                {
                    throw new Exception(errorMsg, ex);
                }
            }
            catch (Exception ex)
            {
                // 其他錯誤
                string errorMsg = $"HTTP request failed: {url} - {ex.Message}";
                _logger.WriteLog($"[ERROR] {errorMsg}");
                _logger.WriteLog(ex);

                if (continueOnError)
                {
                    return $"{{\"error\": \"exception\", \"message\": \"{ex.Message}\"}}";
                }
                else
                {
                    throw new Exception(errorMsg, ex);
                }
            }
        }

        /// <summary>
        /// 使用指定的配置建立 Motion Start 請求的 Payload
        /// </summary>
        /// <param name="config">Motion 配置物件</param>
        /// <param name="unitType">設備類型（例如：TRB, STG, ALN）</param>
        /// <param name="unitId">設備編號</param>
        /// <param name="motion">動作類型</param>
        /// <param name="startTime">開始時間戳記（Unix time milliseconds）</param>
        /// <returns>包含完整 Motion Start 資訊的匿名物件</returns>
        private static object CreateStartPayload(MotionConfig config, string unitType, int unitId, string motion, long startTime)
        {
            return new
            {
                unitType = unitType,
                unitId = unitId,
                tool_id = string.IsNullOrEmpty(config.ToolId) ? $"{unitType}_Tool" : config.ToolId,
                machine_name = string.IsNullOrEmpty(config.MachineName) ? $"{unitType}_Station" : config.MachineName,
                software_version = config.SoftwareVersion,
                effm_number = string.IsNullOrEmpty(config.EffmNumber) ? $"{unitType}_{unitId}" : config.EffmNumber,
                recipe_name = config.RecipeName,
                foup_id = config.FoupId,
                panel_id = config.PanelId,
                lot_id = config.LotId,
                part6 = config.Part6,
                motion_type = motion,
                start_time = startTime,
                timeout = config.Timeout
            };
        }

        /// <summary>
        /// 觸發 Motion 事件
        /// </summary>
        /// <param name="args">Motion 事件參數</param>
        private void OnMotionEventSent(MotionEventArgs args)
        {
            MotionEventSent?.Invoke(this, args);
        }

        #endregion

        #region Instance Methods

        /// <summary>
        /// 發送 Motion Start 事件（實例方法）
        /// </summary>
        /// <param name="unitType">設備類型（例如：TRB, STG, ALN）</param>
        /// <param name="unitId">設備編號</param>
        /// <param name="motion">動作類型</param>
        /// <returns>非同步任務</returns>
        public async Task SendMotionStart(string unitType, int unitId, string motion)
        {
            var startUrl = $"{baseUrl}/api/motion/start";
            var startTime = GetCurrentTimestamp();

            var startPayload = CreateStartPayload(instanceConfig, unitType, unitId, motion, startTime);

            _logger.WriteLog($"==> Start {unitType}#{unitId} motion '{motion}'");
            var response = await HttpPostJson(startUrl, startPayload, instanceConfig.HttpTimeoutSeconds, instanceConfig.ContinueOnError);
            _logger.WriteLog($"Start response: {response}");

            // Raise event
            OnMotionEventSent(new MotionEventArgs
            {
                UnitType = unitType,
                UnitId = unitId,
                Motion = motion,
                Response = response,
                IsStart = true,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 發送 Motion End 事件（實例方法）
        /// </summary>
        /// <param name="unitType">設備類型（例如：TRB, STG, ALN）</param>
        /// <param name="unitId">設備編號</param>
        /// <returns>非同步任務</returns>
        public async Task SendMotionEnd(string unitType, int unitId)
        {
            var endUrl = $"{baseUrl}/api/motion/end";

            var endPayload = new
            {
                unitType = unitType,
                unitId = unitId,
                end_time = GetCurrentTimestamp()
            };

            _logger.WriteLog($"==> End {unitType}#{unitId} motion");
            var response = await HttpPostJson(endUrl, endPayload, instanceConfig.HttpTimeoutSeconds, instanceConfig.ContinueOnError);
            _logger.WriteLog($"End response: {response}");

            // Raise event
            OnMotionEventSent(new MotionEventArgs
            {
                UnitType = unitType,
                UnitId = unitId,
                Motion = "",
                Response = response,
                IsStart = false,
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region Static Methods (向后兼容，保留原有功能)


        /// <summary>
        /// 發送 Motion Start 事件（靜態方法，向後兼容）
        /// </summary>
        /// <param name="baseUrl">API 基礎 URL</param>
        /// <param name="unitType">設備類型</param>
        /// <param name="unitId">設備編號</param>
        /// <param name="motion">動作類型</param>
        /// <returns>非同步任務</returns>
        public static async Task SendMotionStart(string baseUrl, string unitType, int unitId, string motion)
        {
            var startUrl = $"{baseUrl}/api/motion/start";
            var startTime = GetCurrentTimestamp();

            var startPayload = CreateStartPayload(defaultConfig, unitType, unitId, motion, startTime);

            _logger.WriteLog($"==> Start {unitType}#{unitId} motion '{motion}'");
            var startRes = await HttpPostJson(startUrl, startPayload, defaultConfig.HttpTimeoutSeconds, defaultConfig.ContinueOnError);
            _logger.WriteLog($"Start response: {startRes}");
        }

        /// <summary>
        /// 發送 Motion End 事件（靜態方法，向後兼容）
        /// </summary>
        /// <param name="baseUrl">API 基礎 URL</param>
        /// <param name="unitType">設備類型</param>
        /// <param name="unitId">設備編號</param>
        /// <returns>非同步任務</returns>
        public static async Task SendMotionEnd(string baseUrl, string unitType, int unitId)
        {
            var endUrl = $"{baseUrl}/api/motion/end";

            var endPayload = new
            {
                unitType = unitType,
                unitId = unitId,
                end_time = GetCurrentTimestamp()
            };

            _logger.WriteLog($"==> End {unitType}#{unitId} motion");
            var endRes = await HttpPostJson(endUrl, endPayload, defaultConfig.HttpTimeoutSeconds, defaultConfig.ContinueOnError);
            _logger.WriteLog($"End response: {endRes}");
        }

        /// <summary>
        /// 根據布林旗標發送 Motion 事件（靜態方法）
        /// </summary>
        /// <param name="baseUrl">API 基礎 URL</param>
        /// <param name="unitType">設備類型</param>
        /// <param name="unitId">設備編號</param>
        /// <param name="motion">動作類型</param>
        /// <param name="isStart">true = Start, false = End</param>
        /// <returns>非同步任務</returns>
        public static async Task SendMotionEvent(string baseUrl, string unitType, int unitId, string motion, bool isStart)
        {
            if (isStart)
            {
                await SendMotionStart(baseUrl, unitType, unitId, motion);
            }
            else
            {
                await SendMotionEnd(baseUrl, unitType, unitId);
            }
        }

        #endregion
    }
}
