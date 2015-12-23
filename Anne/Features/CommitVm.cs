﻿using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Anne.Foundation;
using Anne.Foundation.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Anne.Features
{
    public class CommitVm : ViewModelBase
    {
        public string Message => _model.Message;
        public string MessageShort => _model.MessageShort;
        public string Parents => string.Join(", ", _model.ParentShaShorts);
        public string Hash => $"{_model.Sha} [{_model.ShaShort}]";
        public string Auther => $"{_model.AutherName} <{_model.AutherEmail}>";
        public string Date => _model.When.ToString("F");

        #region AutherImage

        private BitmapImage _autherImage;

        public BitmapImage AutherImage
        {
            get
            {
                if (_autherImage == null)
                    _autherImage = GravatarLoader.GetFromCache(_model.AutherEmail);

                if (_autherImage != null)
                    return _autherImage;

                if (_isDownloading)
                    return null;

                {
                    _isDownloading = true;
                    Task.Run(
                        () =>
                        {
                            AutherImage = GravatarLoader.Get(_model.AutherEmail);
                            _isDownloading = false;
                        });
                }

                return null;
            }
            set { SetProperty(ref _autherImage, value); }
        }

        #endregion

        private ReadOnlyReactiveCollection<string> _diff;
        public ReadOnlyReactiveCollection<string> Diff
        {
            get
            {
                return _diff ?? (_diff = _model.FilePatches
                    .ToReadOnlyReactiveCollection(x => x.Path)
                    .AddTo(MultipleDisposable));
            }
        }

        private volatile bool _isDownloading;

        private readonly Model.Git.Commit _model;

        public CommitVm(Model.Git.Commit model)
        {
            Debug.Assert(model != null);
            _model = model;
        }
    }
}