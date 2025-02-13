﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Amib.Threading;

namespace Aurora.Modules;

public abstract class AuroraModule : IDisposable
{
    private static readonly SmartThreadPool ModuleThreadPool = new(new STPStartInfo
    {
        AreThreadsBackground = true,
        ThreadPriority = ThreadPriority.BelowNormal,
    })
    {
        Name = "Initialize Threads",
        Concurrency = 25,
        MaxThreads = 8,
    };

    private TaskCompletionSource? _taskSource;

    private Task QueueInit(Func<Task> action)
    {
        _taskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ModuleThreadPool.QueueWorkItem(WorkItemCallback, null);
        if (ModuleThreadPool.IsIdle || ModuleThreadPool.ActiveThreads == 0)
        {
            ModuleThreadPool.Start();
        }

        return _taskSource.Task;

        Task WorkItemCallback(object _)
        {
            Global.logger.Debug("Started module: {Module}", GetType());
            return action().ContinueWith(t =>
            {
                _taskSource.SetResult();
                Global.logger.Debug("Finished loading module: {Module}", GetType());

                return t;
            });
        }
    }

    public virtual Task InitializeAsync()
    {
        return QueueInit(InitAwait);
    }

    private async Task InitAwait()
    {
        try
        {
            await Initialize();
        }
        catch (Exception e)
        {
            Global.logger.Fatal(e, "Module {Type} failed to initialize", GetType());
        }
    }

    protected abstract Task Initialize();
    public abstract Task DisposeAsync();
    public abstract void Dispose();
}