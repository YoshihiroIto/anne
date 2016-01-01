﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Anne.Foundation;
using Anne.Foundation.Mvvm;
using LibGit2Sharp;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using StatefulModel;
using StatefulModel.EventListeners;

namespace Anne.Model.Git
{
    public class Repository : ModelBase
    {
        // ブランチ
        public ReadOnlyReactiveCollection<Branch> Branches { get; }

        // コミット
        public ReactiveProperty<IEnumerable<Commit>> Commits { get; }

        // ジョブキュー
        public ReactiveCollection<string> JobSummries => _jobQueue.JobSummries;
        public ReadOnlyReactiveProperty<string> WorkingJob { get; private set; }
        public event EventHandler<ExceptionEventArgs> JobExecutingException;

        // ファイルステータス
        public FileStatus FileStatus { get; }

        //
        public string Path { get; }

        // 内部状態
        internal LibGit2Sharp.Repository Internal { get; }
        private readonly JobQueue _jobQueue = new JobQueue();

        public Repository(string path)
        {
            Path = path;
            Internal = new LibGit2Sharp.Repository(path).AddTo(MultipleDisposable);

            // ファイルステータス
            FileStatus = new FileStatus(this)
                .AddTo(MultipleDisposable);

            // ジョブキュー
            _jobQueue.AddTo(MultipleDisposable);
            WorkingJob = _jobQueue.WorkingJob
                .ToReadOnlyReactiveProperty(eventScheduler: Scheduler.Immediate)
                .AddTo(MultipleDisposable);

            Observable.FromEventPattern<ExceptionEventArgs>(_jobQueue, nameof(JobQueue.JobExecutingException))
                .Select(x => x.EventArgs)
                .Subscribe(e => JobExecutingException?.Invoke(this, e))
                .AddTo(MultipleDisposable);

            Branches = Internal.Branches
                .ToReadOnlyReactiveCollection(
                    Internal.Branches.ToCollectionChanged<LibGit2Sharp.Branch>(),
                    x => new Branch(x, Internal),
                    Scheduler.Immediate)
                .AddTo(MultipleDisposable);

            {
                Commits = new ReactiveProperty<IEnumerable<Commit>>(
                    Scheduler.Immediate,
                    Internal.Commits.Select(x => new Commit(this, x)).Memoize())
                    .AddTo(MultipleDisposable);

                new AnonymousDisposable(() => Commits.Value.ForEach(x => x.Dispose()))
                    .AddTo(MultipleDisposable);
            }

            {
                var watcher = new FileWatcher(System.IO.Path.Combine(Path, @".git\refs"))
                    .AddTo(MultipleDisposable);

                new EventListener<FileSystemEventHandler>(
                    h => watcher.FileUpdated += h,
                    h => watcher.FileUpdated -= h,
                    (s, e) =>
                    {
                        var old = Commits.Value;
                        Commits.Value = Internal.Commits.Select(x => new Commit(this, x)).Memoize();
                        old.ForEach(x => x.Dispose());
                    })
                    .AddTo(MultipleDisposable);

                watcher.Start();
            }
        }

        private void UpdateBranchProps()
        {
            Branches.ForEach(x => x.UpdateProps());
        }

        public void Fetch(string remoteName)
        {
            _jobQueue.AddJob(
                $"Fetch: {remoteName}",
                () =>
                {
                    var remote = Internal.Network.Remotes[remoteName];
                    Internal.Network.Fetch(remote);
                });
        }

        public void FetchAll()
        {
            Internal.Network.Remotes.Select(r => r.Name).ForEach(Fetch);
        }

        public void Stage(params string[] paths)
        {
            _jobQueue.AddJob(
                $"Stage: {string.Join(",", paths)}",
                () => Internal.Stage(paths));
        }

        public void Unstage(params string[] paths)
        {
            _jobQueue.AddJob(
                $"Unstage: {string.Join(",", paths)}",
                () => Internal.Unstage(paths));
        }

        public void Commit(string message)
        {
            _jobQueue.AddJob(
                $"Commit: {message}",
                () =>
                {
                    var author = Internal.Config.BuildSignature(DateTimeOffset.Now);
                    Internal.Commit(message, author, author);
                });
        }

        public void DiscardChanges(IEnumerable<string> paths)
        {
            var enumerable = paths as string[] ?? paths.ToArray();
            _jobQueue.AddJob(
                $"DiscardChanges: {string.Join(",", enumerable)}",
                () =>
                {
                    var opts = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };

                    Internal.CheckoutPaths("HEAD", enumerable, opts);
                });
        }

        public void Reset(ResetMode mode, string sha)
        {
            _jobQueue.AddJob(
                $"Reset: {mode} {sha}",
                () =>
                {
                    Debug.WriteLine( $"Reset: {mode} {sha}" );
                });
        }

        public void Pull()
        {
            Debug.WriteLine("Pull() -- 未実装");
        }

        public void Push()
        {
            _jobQueue.AddJob(
                "Push",
                () =>
                {
                    var currentBrunch = Internal.Branches.FirstOrDefault(x => x.IsCurrentRepositoryHead);
                    if (currentBrunch == null)
                        return;

                    Internal.Network.Push(currentBrunch);
                });
        }

        public void AddJob(string summry, Action action)
        {
            _jobQueue.AddJob(summry, action);
        }

        public void ExecuteJobSync(string summry, Action action)
        {
            // todo:Mutexが望む動作をしてくれない。要調査

            // var mutex = new Mutex();
            var done = false;

            _jobQueue.AddJob(summry, () =>
            {
                action();

                // mutex.ReleaseMutex();
                done = true;
            });

            // mutex.WaitOne();
            while (done == false)
                Thread.Sleep(0);
        }

        #region Test

        public void CheckoutTest()
        {
            _jobQueue.AddJob(
                "Checkout",
                () =>
                {
                    var srcBranch = Branches.FirstOrDefault(b => b.Name == "origin/refactoring");
                    srcBranch?.Checkout();
                    UpdateBranchProps();
                });
        }

        public void RemoveTest()
        {
            _jobQueue.AddJob(
                "Remove",
                () =>
                {
                    var srcBranch = Branches.FirstOrDefault(b => b.Name == "refactoring");
                    srcBranch?.Remove();
                });
        }

        public void SwitchTest(string branchName)
        {
            _jobQueue.AddJob(
                $"Switch: {branchName}",
                () =>
                {
                    var branch = Branches.FirstOrDefault(b => b.Name == branchName);
                    branch?.Switch();
                    UpdateBranchProps();
                });
        }

        #endregion
    }
}