﻿// Copyright (c) Richasy. All rights reserved.

using System.Linq;
using System.Threading.Tasks;
using Bili.DI.Container;
using Bili.Lib.Interfaces;
using Bili.Models.Data.Local;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces.Account;
using Bili.ViewModels.Interfaces.Core;
using Bili.ViewModels.Interfaces.Video;
using Bili.ViewModels.Uwp.Base;
using CommunityToolkit.Mvvm.Input;
using Windows.UI.Core;

namespace Bili.ViewModels.Uwp.Account
{
    /// <summary>
    /// 稍后再看页面视图模型.
    /// </summary>
    public sealed partial class ViewLaterPageViewModel : InformationFlowViewModelBase<IVideoItemViewModel>, IViewLaterPageViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewLaterPageViewModel"/> class.
        /// </summary>
        public ViewLaterPageViewModel(
            IAccountProvider accountProvider,
            IResourceToolkit resourceToolkit,
            INavigationViewModel navigationViewModel,
            CoreDispatcher dispatcher)
            : base(dispatcher)
        {
            _accountProvider = accountProvider;
            _resourceToolkit = resourceToolkit;
            _navigationViewModel = navigationViewModel;

            ClearCommand = new AsyncRelayCommand(ClearAllAsync);
            PlayAllCommand = new RelayCommand(PlayAll);

            AttachIsRunningToAsyncCommand(p => IsClearing = p, ClearCommand);
        }

        /// <inheritdoc/>
        protected override void BeforeReload()
        {
            _accountProvider.ResetViewLaterStatus();
            IsEmpty = false;
            _isEnd = false;
        }

        /// <inheritdoc/>
        protected override async Task GetDataAsync()
        {
            if (_isEnd)
            {
                return;
            }

            var data = await _accountProvider.GetViewLaterListAsync();
            foreach (var item in data.Items)
            {
                var videoVM = Locator.Instance.GetService<IVideoItemViewModel>();
                videoVM.InjectData(item);
                videoVM.InjectAction(vm => RemoveVideo(vm));
                Items.Add(videoVM);
            }

            _isEnd = Items.Count == data.TotalCount;
            IsEmpty = Items.Count == 0;
        }

        /// <inheritdoc/>
        protected override string FormatException(string errorMsg)
            => $"{_resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.RequestViewLaterFailed)}\n{errorMsg}";

        private async Task ClearAllAsync()
        {
            var result = await _accountProvider.ClearViewLaterAsync();
            if (result)
            {
                TryClear(Items);
                _ = ReloadCommand.ExecuteAsync(null);
            }
        }

        private void PlayAll()
        {
            if (Items.Count > 1)
            {
                _navigationViewModel.NavigateToPlayView(Items.Select(p => p.Data).ToList());
            }
            else if (Items.Count > 0)
            {
                _navigationViewModel.NavigateToPlayView(GetSnapshot(Items.First()));
            }
        }

        private PlaySnapshot GetSnapshot(IVideoItemViewModel vm)
        {
            var info = vm.Data;
            return new PlaySnapshot(info.Identifier.Id, "0", Models.Enums.VideoType.Video);
        }

        private void RemoveVideo(IVideoItemViewModel vm)
        {
            Items.Remove(vm);
            IsEmpty = Items.Count == 0;
        }
    }
}
