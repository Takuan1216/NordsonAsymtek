using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RorzeApi.Class
{
    /// <summary>
    /// Motion tracking helper - 最小侵入式的 GRPC 集成方案
    /// 用于追踪设备动作并自动发送 Motion Start/End
    /// </summary>
    public static class MotionTracker
    {
        private static readonly ConcurrentDictionary<string, GRPC> _grpcClients = new ConcurrentDictionary<string, GRPC>();
        private static readonly ConcurrentDictionary<string, MotionContext> _activeMotions = new ConcurrentDictionary<string, MotionContext>();
        private static string _defaultBaseUrl = "http://localhost:61723";

        /// <summary>
        /// Motion context - 记录当前动作的上下文
        /// </summary>
        private class MotionContext
        {
            public string UnitType { get; set; }
            public int UnitId { get; set; }
            public string Motion { get; set; }
            public DateTime StartTime { get; set; }
        }

        #region 初始化和配置

        /// <summary>
        /// 设置默认 API URL
        /// </summary>
        public static void SetDefaultBaseUrl(string baseUrl)
        {
            _defaultBaseUrl = baseUrl;
        }

        /// <summary>
        /// 获取或创建 GRPC 客户端
        /// </summary>
        private static GRPC GetOrCreateClient(string key, string baseUrl = null)
        {
            return _grpcClients.GetOrAdd(key, k => new GRPC(baseUrl ?? _defaultBaseUrl));
        }

        /// <summary>
        /// 更新指定设备的配置
        /// </summary>
        public static void UpdateConfig(string unitType, int unitId,
            string recipeName = null, string foupId = null,
            string panelId = null, string lotId = null, string part6 = null)
        {
            string key = $"{unitType}_{unitId}";
            var client = GetOrCreateClient(key);
            client.UpdateConfig(recipeName, foupId, panelId, lotId, part6);
        }

        /// <summary>
        /// 批量更新配置
        /// </summary>
        public static void UpdateConfig(string unitType, int unitId, MotionConfig config)
        {
            string key = $"{unitType}_{unitId}";
            var client = GetOrCreateClient(key);
            client.UpdateConfig(c =>
            {
                c.RecipeName = config.RecipeName;
                c.FoupId = config.FoupId;
                c.PanelId = config.PanelId;
                c.LotId = config.LotId;
                c.Part6 = config.Part6;
            });
        }

        #endregion

        #region 核心方法 - 包装器

        /// <summary>
        /// 执行带 Motion 追踪的操作（推荐使用）
        /// </summary>
        /// <param name="unitType">设备类型 (Robot, Loadport, Aligner)</param>
        /// <param name="unitId">设备 ID</param>
        /// <param name="motion">动作类型</param>
        /// <param name="action">要执行的动作</param>
        /// <param name="onComplete">完成时的回调（可选）</param>
        public static async Task TrackMotion(
            string unitType,
            int unitId,
            string motion,
            Action action,
            Action onComplete = null)
        {
            string key = $"{unitType}_{unitId}";
            var client = GetOrCreateClient(key);

            try
            {
                // 1. 发送 Motion Start
                await client.SendMotionStart(unitType, unitId, motion);

                // 2. 记录上下文
                _activeMotions[key] = new MotionContext
                {
                    UnitType = unitType,
                    UnitId = unitId,
                    Motion = motion,
                    StartTime = DateTime.Now
                };

                // 3. 执行实际动作
                action?.Invoke();

                // 4. 执行完成回调
                onComplete?.Invoke();

                // 5. 发送 Motion End
                await client.SendMotionEnd(unitType, unitId);

                // 6. 清理上下文
                _activeMotions.TryRemove(key, out _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MotionTracker] Error: {ex.Message}");
                // 即使出错也要尝试发送 End
                try
                {
                    await client.SendMotionEnd(unitType, unitId);
                }
                catch { }
                _activeMotions.TryRemove(key, out _);
                throw;
            }
        }

        /// <summary>
        /// 异步版本 - 执行带 Motion 追踪的操作
        /// </summary>
        public static async Task TrackMotionAsync(
            string unitType,
            int unitId,
            string motion,
            Func<Task> actionAsync,
            Func<Task> onCompleteAsync = null)
        {
            string key = $"{unitType}_{unitId}";
            var client = GetOrCreateClient(key);

            try
            {
                // 1. 发送 Motion Start
                await client.SendMotionStart(unitType, unitId, motion);

                // 2. 记录上下文
                _activeMotions[key] = new MotionContext
                {
                    UnitType = unitType,
                    UnitId = unitId,
                    Motion = motion,
                    StartTime = DateTime.Now
                };

                // 3. 执行实际动作
                if (actionAsync != null)
                    await actionAsync();

                // 4. 执行完成回调
                if (onCompleteAsync != null)
                    await onCompleteAsync();

                // 5. 发送 Motion End
                await client.SendMotionEnd(unitType, unitId);

                // 6. 清理上下文
                _activeMotions.TryRemove(key, out _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MotionTracker] Error: {ex.Message}");
                try
                {
                    await client.SendMotionEnd(unitType, unitId);
                }
                catch { }
                _activeMotions.TryRemove(key, out _);
                throw;
            }
        }

        #endregion

        #region 手动控制方法（高级用法）

        /// <summary>
        /// 手动发送 Motion Start（适用于手动控制的场景）
        /// </summary>
        public static async Task SendStart(string unitType, int unitId, string motion)
        {
            string key = $"{unitType}_{unitId}";
            var client = GetOrCreateClient(key);

            await client.SendMotionStart(unitType, unitId, motion);

            _activeMotions[key] = new MotionContext
            {
                UnitType = unitType,
                UnitId = unitId,
                Motion = motion,
                StartTime = DateTime.Now
            };
        }

        /// <summary>
        /// 手动发送 Motion End（适用于手动控制的场景）
        /// </summary>
        public static async Task SendEnd(string unitType, int unitId)
        {
            string key = $"{unitType}_{unitId}";
            var client = GetOrCreateClient(key);

            await client.SendMotionEnd(unitType, unitId);

            _activeMotions.TryRemove(key, out _);
        }

        /// <summary>
        /// 检查是否有正在进行的动作
        /// </summary>
        public static bool IsMotionActive(string unitType, int unitId)
        {
            string key = $"{unitType}_{unitId}";
            return _activeMotions.ContainsKey(key);
        }

        /// <summary>
        /// 获取当前动作信息
        /// </summary>
        public static string GetCurrentMotion(string unitType, int unitId)
        {
            string key = $"{unitType}_{unitId}";
            if (_activeMotions.TryGetValue(key, out var context))
            {
                return context.Motion;
            }
            return null;
        }

        #endregion

        #region 便捷的扩展方法支持

        /// <summary>
        /// 为 Event 提供便捷的注册方法
        /// </summary>
        public static EventHandler<T> WrapWithMotionEnd<T>(
            string unitType,
            int unitId,
            EventHandler<T> originalHandler) where T : EventArgs
        {
            return async (sender, e) =>
            {
                // 先发送 Motion End
                await SendEnd(unitType, unitId);

                // 再调用原始处理器
                originalHandler?.Invoke(sender, e);
            };
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 清理所有客户端（程序退出时调用）
        /// </summary>
        public static void Cleanup()
        {
            _grpcClients.Clear();
            _activeMotions.Clear();
        }

        #endregion
    }
}
