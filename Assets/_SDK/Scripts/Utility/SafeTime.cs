using UnityEngine;
using System.Threading;

namespace Nexzap.Base.Utils
{
    /// <summary>
    /// SafeTime đảm bảo luôn có thể lấy Time.realtimeSinceStartup từ bất kỳ thread nào.
    /// Nếu không ở main thread, nó trả về giá trị gần nhất được cập nhật từ main thread.
    /// </summary>
    [DefaultExecutionOrder(-9999)] // chạy rất sớm trong frame
    public class SafeTime : MonoSingleton<SafeTime>
    {
        private static readonly int mainThreadId;
        private static float lastRealtimeSinceStartup;

        static SafeTime()
        {
            // Ghi lại thread ID của main thread khi Unity bắt đầu
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private void Update()
        {
            // Cập nhật liên tục giá trị mới nhất mỗi frame
            lastRealtimeSinceStartup = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Luôn trả về giá trị Time.realtimeSinceStartup chính xác nếu ở main thread,
        /// hoặc giá trị gần nhất được lưu lại nếu đang ở background thread.
        /// </summary>
        public static float RealtimeSinceStartupSafe
        {
            get
            {
                if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
                {
                    // Cập nhật luôn nếu đang ở main thread
                    lastRealtimeSinceStartup = Time.realtimeSinceStartup;
                    return lastRealtimeSinceStartup;
                }
                else
                {
                    // Trả về giá trị gần nhất đã ghi lại
                    return lastRealtimeSinceStartup;
                }
            }
        }
    }
}
