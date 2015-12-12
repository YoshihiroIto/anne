﻿using System.Linq;
using Anne.Foundation;
using Anne.Foundation.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;
using StatefulModel;

namespace Anne.Model.Git
{
    public class Repository : ModelBase
    {
        // ブランチ
        public ReadOnlyReactiveCollection<Branch> Branches { get; }
        public IFilteredReadOnlyObservableCollection<Branch> LocalBranches { get; }
        public IFilteredReadOnlyObservableCollection<Branch> RemoteBranches { get; }

        // コミット
        public ReactiveProperty<Commit[]> Commits { get; }

        // 処理キュー
        public ReadOnlyReactiveCollection<string> JobSummries { get; private set; }
        public ReactiveProperty<string> WorkingJob { get; private set; }

        // 
        private readonly LibGit2Sharp.Repository _internal;
        private readonly JobQueue _reposJobQueue = new JobQueue();

        public Repository(string path)
        {
            MultipleDisposable.Add(_reposJobQueue);
            JobSummries = _reposJobQueue.JobSummries;
            WorkingJob = _reposJobQueue.WorkingJob;

            _internal = new LibGit2Sharp.Repository(path).AddTo(MultipleDisposable);

            Branches = _internal.Branches
                .ToReadOnlyReactiveCollection(
                    _internal.Branches.ToCollectionChanged<LibGit2Sharp.Branch>(),
                    x => new Branch(x, _internal))
                .AddTo(MultipleDisposable);

            LocalBranches = Branches
                .ToFilteredReadOnlyObservableCollection(x => !x.IsRemote.Value)
                .AddTo(MultipleDisposable);

            RemoteBranches = Branches
                .ToFilteredReadOnlyObservableCollection(x => x.IsRemote.Value)
                .AddTo(MultipleDisposable);

            // コミット
            Commits = new ReactiveProperty<Commit[]>(_internal.Commits
                .Select(x => new Commit(x)).ToArray())
                .AddTo(MultipleDisposable);

            MultipleDisposable.Add(new AnonymousDisposable(() =>
                Commits.Value.ForEach(x => x.Dispose())
                ));
        }

        private void UpdateBranchProps()
        {
            Branches.ForEach(x => x.UpdateProps());
        }

        #region Test

        public void CheckoutTest()
        {
            _reposJobQueue.AddJob(
                "Checkout",
                () =>
                {
                    var srcBranch = RemoteBranches.FirstOrDefault(b => b.Name.Value == "origin/refactoring");
                    srcBranch?.Checkout();
                    UpdateBranchProps();
                });
        }

        public void RemoveTest()
        {
            _reposJobQueue.AddJob(
                "Remove",
                () =>
                {
                    var srcBranch = LocalBranches.FirstOrDefault(b => b.Name.Value == "refactoring");
                    srcBranch?.Remove();
                });
        }

        public void SwitchTest(string branchName)
        {
            _reposJobQueue.AddJob(
                $"Switch: {branchName}",
                () =>
                {
                    var branch = LocalBranches.FirstOrDefault(b => b.Name.Value == branchName);
                    branch?.Switch();
                    UpdateBranchProps();
                });
        }

        public void FetchTest(string remoteName)
        {
            _reposJobQueue.AddJob(
                $"Fetch: {remoteName}",
                () =>
                {
                    var remote = _internal.Network.Remotes[remoteName];
                    _internal.Network.Fetch(remote);
                });
        }

        #endregion
    }
}