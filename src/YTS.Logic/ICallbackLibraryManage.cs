using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace YTS.Logic
{
    public interface ICallbackLibraryUseConfig<T>
    {
        /// <summary>
        /// 注册回调
        /// </summary>
        /// <param name="callback">需要注册的回调对象</param>
        /// <returns>返回这个回调可自释放自主取消订阅的释放对象</returns>
        IDisposable RegisterCallback(T callback);
    }
    public interface ICallbackLibraryManage<T>
    {
        /// <summary>
        /// 注册回调
        /// </summary>
        /// <param name="callback">需要注册的回调对象</param>
        /// <returns>返回这个回调可自释放自主取消订阅的释放对象</returns>
        IDisposable RegisterCallback(T callback);

        /// <summary>
        /// 返回当前状态还是订阅中的回调对象
        /// </summary>
        /// <returns>结果列表</returns>
        T[] GetSubscribedCallbacks();
    }

    public class CallbackLibraryManage<T> : ICallbackLibraryManage<T>
    {
        private readonly List<CallbackSubscription> _subs;
        private readonly object _subsLock = new object();

        public CallbackLibraryManage()
        {
            _subs = new List<CallbackSubscription>();
        }

        public T[] GetSubscribedCallbacks()
        {
            CallbackSubscription[] snapshot;
            lock (_subsLock) snapshot = _subs.ToArray();
            T[] rlist = snapshot
                .Where(b => !b.IsDisposed && b.Callback != null)
                .Select(b => b.Callback)
                .ToArray();
            return rlist;
        }

        public IDisposable RegisterCallback(T callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            var sub = new CallbackSubscription(this, callback);
            lock (_subsLock) _subs.Add(sub);
            return sub;   // ← 返回"退订凭证"
        }

        private void RemoveSubscription(CallbackSubscription sub)
        {
            if (sub == null) return;
            lock (_subsLock) _subs.Remove(sub);
        }

        /// <summary>
        /// 订阅凭证。Dispose 时自动从 Service 的订阅表中移除。
        /// 幂等：多次 Dispose 只生效一次。
        /// </summary>
        private sealed class CallbackSubscription : IDisposable
        {
            private readonly CallbackLibraryManage<T> _owner;
            private readonly T _callback;
            private int _disposed;   // 0=有效，1=已退订

            public CallbackSubscription(CallbackLibraryManage<T> owner, T callback)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                if (callback == null)
                    throw new ArgumentNullException(nameof(callback));
                _callback = callback;
            }

            public T Callback => _callback;
            public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

            public void Dispose()
            {
                // ⭐ 幂等：只有第一次调用才真正移除
                if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
                try { _owner.RemoveSubscription(this); }
                catch { }
            }
        }
    }
}
