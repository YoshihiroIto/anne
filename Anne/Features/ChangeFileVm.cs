﻿using System.Diagnostics;
using Anne.Diff;
using Anne.Features.Interfaces;
using Anne.Foundation.Mvvm;
using Anne.Model.Git;
using LibGit2Sharp;

namespace Anne.Features
{
    public class ChangeFileVm : ViewModelBase, IFileDiffVm
    {
        // IFileDiffVm
        public string Path => _model.Path;
        public string Diff { get; set; }
        public DiffLine[] DiffLines { get; set;  }
        public int LinesAdded => _model.LinesAdded;
        public int LinesDeleted => _model.LinesDeleted;
        public Mode Mode => _model.Mode;

        private readonly ChangeFile _model;

        public ChangeFileVm(ChangeFile model)
        {
            Debug.Assert(model != null);

            _model = model;

            this.MakeDiff(_model.Patch);
        }
    }
}