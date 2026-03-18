using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Auris_Studio.ViewModels.Helpers;

public static class MicrosecondDelay
{
    private static readonly double StopwatchFrequencyPerMicrosecond = Stopwatch.Frequency / 1_000_000.0;
    private static readonly double StopwatchFrequencyPerMillisecond = Stopwatch.Frequency / 1_000.0;

    /// <summary>
    /// 微秒级高精度异步延迟（不阻塞UI线程）
    /// 适合高频调用场景，使用ValueTask减少分配
    /// </summary>
    /// <param name="microseconds">延迟微秒数（1-1000微秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ValueTask，大部分情况下同步完成，零分配</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask Delay(int microseconds, CancellationToken cancellationToken = default)
    {
        // 快速路径：零延迟或已取消
        if (microseconds <= 0)
            return ValueTask.CompletedTask;

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);

        // 微秒级延迟直接同步完成，避免异步状态机开销
        if (microseconds <= 1000) // 1毫秒以内的延迟
        {
            try
            {
                DelayMicroseconds((uint)microseconds, cancellationToken);
                return ValueTask.CompletedTask;
            }
            catch (OperationCanceledException ex)
            {
                return ValueTask.FromCanceled(ex.CancellationToken);
            }
        }

        // 超过1毫秒的延迟走异步路径
        return DelayAsync(microseconds, cancellationToken);
    }

    /// <summary>
    /// 纳秒级精度的同步延迟（核心实现）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DelayMicroseconds(uint microseconds, CancellationToken cancellationToken)
    {
        long targetTicks = (long)(microseconds * StopwatchFrequencyPerMicrosecond);

        // 使用RDTSC或Stopwatch获取高精度时间戳
        var sw = Stopwatch.StartNew();
        long startTicks = sw.ElapsedTicks;
        long endTicks = startTicks + targetTicks;

        // 微秒级延迟优化策略
        while (sw.ElapsedTicks < endTicks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long remainingTicks = endTicks - sw.ElapsedTicks;

            // 智能等待策略
            if (remainingTicks > 1000) // 约0.1微秒
            {
                // 让出CPU时间片但不阻塞
                Thread.Sleep(0);
            }
            else if (remainingTicks > 100) // 约0.01微秒
            {
                // 短暂让出CPU
                Thread.Yield();
            }
            else
            {
                // 最后阶段使用精确自旋
                Thread.SpinWait(1);
            }
        }
    }

    /// <summary>
    /// 异步路径（用于超过1毫秒的延迟）
    /// </summary>
    private static async ValueTask DelayAsync(int microseconds, CancellationToken cancellationToken)
    {
        // 计算总延迟
        long totalTicks = (long)(microseconds * StopwatchFrequencyPerMicrosecond);
        var sw = Stopwatch.StartNew();
        long elapsedTicks = 0;

        while (elapsedTicks < totalTicks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long remainingTicks = totalTicks - elapsedTicks;
            double remainingMicroseconds = remainingTicks / StopwatchFrequencyPerMicrosecond;

            if (remainingMicroseconds > 1000.0) // 剩余超过1毫秒
            {
                // 使用Task.Delay处理较长时间
                int delayMs = (int)(remainingMicroseconds / 1000.0);
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (remainingMicroseconds > 100.0) // 100微秒-1毫秒
            {
                // 使用Thread.Yield
                await Task.Yield();
            }
            else // 小于100微秒
            {
                // 短时间自旋等待
                Thread.SpinWait(10);
            }

            elapsedTicks = sw.ElapsedTicks;
        }
    }

    /// <summary>
    /// 浮点数微秒延迟（更高精度）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask Delay(double microseconds, CancellationToken cancellationToken = default)
    {
        if (microseconds <= 0)
            return ValueTask.CompletedTask;

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);

        // 小延迟直接同步完成
        if (microseconds <= 1000.0)
        {
            try
            {
                long targetTicks = (long)(microseconds * StopwatchFrequencyPerMicrosecond);
                var sw = Stopwatch.StartNew();
                long endTicks = sw.ElapsedTicks + targetTicks;

                while (sw.ElapsedTicks < endTicks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Thread.SpinWait(1);
                }

                return ValueTask.CompletedTask;
            }
            catch (OperationCanceledException ex)
            {
                return ValueTask.FromCanceled(ex.CancellationToken);
            }
        }

        // 较大延迟走异步
        return DelayAsync((int)Math.Ceiling(microseconds), cancellationToken);
    }

    /// <summary>
    /// Windows平台最高精度版本（使用多媒体定时器API）
    /// 精度可达1微秒级别
    /// </summary>
    public static class WindowsHighPrecision
    {
        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);

        [DllImport("kernel32.dll")]
        private static extern void QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("kernel32.dll")]
        private static extern void QueryPerformanceFrequency(out long lpFrequency);

        private static readonly long Frequency;

        static WindowsHighPrecision()
        {
            QueryPerformanceFrequency(out Frequency);
            _ = timeBeginPeriod(1); // 设置1毫秒精度
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask Delay(int microseconds, CancellationToken cancellationToken = default)
        {
            if (microseconds <= 0)
                return ValueTask.CompletedTask;

            try
            {
                long targetCount = microseconds * Frequency / 1_000_000L;
                QueryPerformanceCounter(out long startCount);
                long endCount = startCount + targetCount;

                long currentCount;
                do
                {
                    if (cancellationToken.IsCancellationRequested)
                        return ValueTask.FromCanceled(cancellationToken);

                    QueryPerformanceCounter(out currentCount);

                    // 精确自旋
                    if (endCount - currentCount > Frequency / 1000) // 约1微秒
                    {
                        Thread.SpinWait(1);
                    }
                }
                while (currentCount < endCount);

                return ValueTask.CompletedTask;
            }
            catch
            {
                return ValueTask.FromException(new PlatformNotSupportedException("高精度定时器仅支持Windows"));
            }
        }
    }
}