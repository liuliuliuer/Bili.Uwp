﻿// Copyright (c) Richasy. All rights reserved.

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Bili.DI.Container;
using Bili.Lib.Interfaces;
using Bili.Models.App.Other;
using Bili.Models.Data.Community;
using Bili.Models.Enums.Bili;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces.Community;
using Bili.ViewModels.Interfaces.Core;
using Bili.ViewModels.Uwp.Base;
using CommunityToolkit.Mvvm.Input;
using Windows.UI.Core;

namespace Bili.ViewModels.Uwp.Community
{
    /// <summary>
    /// 主评论模块视图模型.
    /// </summary>
    public sealed partial class CommentMainModuleViewModel : InformationFlowViewModelBase<ICommentItemViewModel>, ICommentMainModuleViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommentMainModuleViewModel"/> class.
        /// </summary>
        public CommentMainModuleViewModel(
            ICommunityProvider communityProvider,
            IResourceToolkit resourceToolkit,
            ICallerViewModel callerViewModel,
            CoreDispatcher dispatcher)
            : base(dispatcher)
        {
            _communityProvider = communityProvider;
            _resourceToolkit = resourceToolkit;
            _callerViewModel = callerViewModel;

            SortCollection = new ObservableCollection<CommentSortHeader>
            {
                new CommentSortHeader(CommentSortType.Hot, _resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.SortByHot)),
                new CommentSortHeader(CommentSortType.Time, _resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.SortByNewest)),
            };

            CurrentSort = SortCollection.First();

            ChangeSortCommand = new RelayCommand<CommentSortHeader>(ChangeSort);
            ResetSelectedCommentCommand = new RelayCommand(UnselectComment);
            SendCommentCommand = new AsyncRelayCommand(SendCommentAsync);

            AttachIsRunningToAsyncCommand(p => IsSending = p, SendCommentCommand);
        }

        /// <inheritdoc/>
        public void SetTarget(string targetId, CommentType type, CommentSortType defaultSort = CommentSortType.Hot)
        {
            TryClear(Items);
            _targetId = targetId;
            _commentType = type;
            var sort = SortCollection.First(p => p.Type == defaultSort);
            CurrentSort = sort;
            InitializeCommand.ExecuteAsync(null);
        }

        /// <inheritdoc/>
        public void ClearData()
        {
            TryClear(Items);
            BeforeReload();
            UnselectComment();
        }

        /// <inheritdoc/>
        protected override void BeforeReload()
        {
            _isEnd = false;
            IsEmpty = false;
            TopComment = null;
            _communityProvider.ResetMainCommentsStatus();
        }

        /// <inheritdoc/>
        protected override string FormatException(string errorMsg)
            => $"{_resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.RequestReplyFailed)}\n{errorMsg}";

        /// <inheritdoc/>
        protected override async Task GetDataAsync()
        {
            if (_isEnd)
            {
                return;
            }

            var data = await _communityProvider.GetCommentsAsync(_targetId, _commentType, CurrentSort.Type);
            _isEnd = data.IsEnd;
            if (data.TopComment != null)
            {
                var top = GetItemViewModel(data.TopComment);
                TopComment = top;
            }

            foreach (var item in data.Comments)
            {
                if (!Items.Any(p => p.Data.Equals(item)))
                {
                    var vm = GetItemViewModel(item);
                    Items.Add(vm);
                }
            }

            IsEmpty = Items.Count == 0 && TopComment == null;
        }

        private void ChangeSort(CommentSortHeader sort)
        {
            CurrentSort = sort;
            ReloadCommand.ExecuteAsync(null);
        }

        private void UnselectComment()
        {
            _selectedComment = null;
            ReplyTip = string.Empty;
        }

        private async Task SendCommentAsync()
        {
            if (IsSending || string.IsNullOrEmpty(ReplyText))
            {
                return;
            }

            var content = ReplyText;
            var replyCommentId = _selectedComment == null ? "0" : _selectedComment.Data.Id;
            var result = await _communityProvider.AddCommentAsync(content, _targetId, _commentType, "0", replyCommentId);
            if (result)
            {
                ReplyText = string.Empty;
                UnselectComment();
                if (CurrentSort.Type == CommentSortType.Time)
                {
                    // 即便评论发送成功也需要等待一点时间才会显示.
                    await Task.Delay(500);
                    _ = ReloadCommand.ExecuteAsync(null);
                }
            }
            else
            {
                _callerViewModel.ShowTip(_resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.AddReplyFailed), Models.Enums.App.InfoType.Error);
            }
        }

        private ICommentItemViewModel GetItemViewModel(CommentInformation information)
        {
            var commentVM = Locator.Instance.GetService<ICommentItemViewModel>();
            commentVM.InjectData(information);
            commentVM.SetDetailAction(vm =>
            {
                RequestShowDetail?.Invoke(this, vm);
            });
            commentVM.SetClickAction(vm =>
            {
                _selectedComment = vm;
                ReplyTip = string.Format(_resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.ReplySomeone), vm.Data.Publisher.User.Name);
            });
            return commentVM;
        }
    }
}
