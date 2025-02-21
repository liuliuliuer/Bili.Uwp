﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Bili.DI.Container;
using Bili.Lib.Interfaces;
using Bili.Models.Data.Community;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces.Account;
using Bili.ViewModels.Uwp.Base;
using CommunityToolkit.Mvvm.Input;
using Windows.UI.Core;

namespace Bili.ViewModels.Uwp.Account
{
    /// <summary>
    /// 我的关注页面视图模型.
    /// </summary>
    public sealed partial class MyFollowsPageViewModel : InformationFlowViewModelBase<IUserItemViewModel>, IMyFollowsPageViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MyFollowsPageViewModel"/> class.
        /// </summary>
        public MyFollowsPageViewModel(
            IAccountProvider accountProvider,
            IResourceToolkit resourceToolkit,
            IAccountViewModel accountViewModel,
            CoreDispatcher dispatcher)
            : base(dispatcher)
        {
            _accountProvider = accountProvider;
            _resourceToolkit = resourceToolkit;
            _accountViewModel = accountViewModel;
            _cache = new Dictionary<string, IEnumerable<IUserItemViewModel>>();
            Groups = new ObservableCollection<FollowGroup>();
            UserName = _accountViewModel.DisplayName;

            SelectGroupCommand = new AsyncRelayCommand<FollowGroup>(SelectGroupAsync);
            AttachIsRunningToAsyncCommand(p => IsSwitching = p, SelectGroupCommand);
            AttachExceptionHandlerToAsyncCommand(DisplayException, SelectGroupCommand);
        }

        /// <inheritdoc/>
        protected override void BeforeReload()
        {
            _accountProvider.ClearMyFollowStatus();
            TryClear(Groups);
            CurrentGroup = null;
            _cache.Clear();
            IsCurrentGroupEmpty = false;
        }

        /// <inheritdoc/>
        protected override async Task GetDataAsync()
        {
            IsCurrentGroupEmpty = false;
            if (Groups.Count == 0)
            {
                // 加载分组.
                var groups = await _accountProvider.GetMyFollowingGroupsAsync();
                groups.ToList().ForEach(p => Groups.Add(p));
                await FakeLoadingAsync();
                CurrentGroup = Groups.FirstOrDefault(p => p.TotalCount > 0) ?? Groups.FirstOrDefault();
            }

            if (CurrentGroup == null
                || CurrentGroup.TotalCount <= Items.Count)
            {
                IsCurrentGroupEmpty = Items.Count == 0;
                return;
            }

            var data = await _accountProvider.GetMyFollowingGroupDetailAsync(CurrentGroup.Id);
            foreach (var item in data)
            {
                if (Items.Any(p => p.User.Equals(item.User)))
                {
                    return;
                }

                var accVM = Locator.Instance.GetService<IUserItemViewModel>();
                accVM.SetInformation(item);
                Items.Add(accVM);
            }

            IsCurrentGroupEmpty = Items.Count == 0;
            _cache.Remove(CurrentGroup.Id);
            _cache.Add(CurrentGroup.Id, Items.ToList());
        }

        /// <inheritdoc/>
        protected override string FormatException(string errorMsg)
            => $"{_resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.RequestFollowsFailed)}\n{errorMsg}";

        private async Task SelectGroupAsync(FollowGroup group)
        {
            CurrentGroup = group;
            TryClear(Items);
            if (_cache.TryGetValue(group.Id, out var cache))
            {
                cache.ToList().ForEach(p => Items.Add(p));
                IsCurrentGroupEmpty = Items.Count == 0;
            }
            else
            {
                await GetDataAsync();
            }
        }
    }
}
