﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Bili.DI.Container;
using Bili.Lib.Interfaces;
using Bili.Models.Enums;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces.Pgc;
using Bili.ViewModels.Uwp.Base;
using Windows.UI.Core;

namespace Bili.ViewModels.Uwp.Pgc
{
    /// <summary>
    /// PGC 内容索引页面视图模型.
    /// </summary>
    public sealed partial class IndexPageViewModel : InformationFlowViewModelBase<ISeasonItemViewModel>, IIndexPageViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexPageViewModel"/> class.
        /// </summary>
        /// <param name="pgcProvider">PGC 服务提供工具.</param>
        /// <param name="resourceToolkit">资源管理工具.</param>
        /// <param name="dispatcher">UI 调度器.</param>
        public IndexPageViewModel(
            IPgcProvider pgcProvider,
            IResourceToolkit resourceToolkit,
            CoreDispatcher dispatcher)
            : base(dispatcher)
        {
            _pgcProvider = pgcProvider;
            _resourceToolkit = resourceToolkit;
            Filters = new ObservableCollection<IIndexFilterViewModel>();
        }

        /// <inheritdoc/>
        public void SetType(PgcType type)
        {
            _type = type;
            PageType = _type switch
            {
                PgcType.Bangumi => _resourceToolkit.GetLocaleString(LanguageNames.Bangumi),
                PgcType.Domestic => _resourceToolkit.GetLocaleString(LanguageNames.DomesticAnime),
                PgcType.Movie => _resourceToolkit.GetLocaleString(LanguageNames.Movie),
                PgcType.Documentary => _resourceToolkit.GetLocaleString(LanguageNames.Documentary),
                PgcType.TV => _resourceToolkit.GetLocaleString(LanguageNames.TV),
                _ => throw new ArgumentException("错误的PGC类型", nameof(type))
            };

            TryClear(Filters);
            TryClear(Items);
            BeforeReload();
        }

        /// <inheritdoc/>
        protected override void BeforeReload()
        {
            _isFinished = false;
            IsEmpty = false;
            _pgcProvider.ResetIndexStatus();
        }

        /// <inheritdoc/>
        protected override async Task GetDataAsync()
        {
            if (Filters.Count == 0)
            {
                await LoadFiltersAsync();
            }

            if (!_isFinished)
            {
                await LoadItemsAsync();
            }
        }

        /// <inheritdoc/>
        protected override string FormatException(string errorMsg)
            => $"{_resourceToolkit.GetLocaleString(LanguageNames.RequestIndexResultFailed)}\n{errorMsg}";

        private async Task LoadFiltersAsync()
        {
            TryClear(Filters);
            var filters = await _pgcProvider.GetPgcIndexFiltersAsync(_type);
            var isAnime = _type == PgcType.Bangumi || _type == PgcType.Domestic;
            foreach (var item in filters)
            {
                var vm = Locator.Instance.GetService<IIndexFilterViewModel>();
                if (isAnime && item.Id == "area")
                {
                    var areaId = _type == PgcType.Bangumi ? "2" : "1,";
                    var selectedItem = item.Conditions.FirstOrDefault(p => p.Id.Contains(areaId));
                    vm.SetData(item, selectedItem);
                }
                else
                {
                    vm.SetData(item);
                }

                Filters.Add(vm);
            }
        }

        private async Task LoadItemsAsync()
        {
            var queryPrameters = new Dictionary<string, string>();
            foreach (var item in Filters)
            {
                if (item.SelectedIndex >= 0)
                {
                    var id = item.Data.Conditions.ToList()[item.SelectedIndex].Id;
                    if (item.Data.Id == "year")
                    {
                        id = Uri.EscapeDataString(id);
                    }

                    queryPrameters.Add(item.Data.Id, id);
                }
            }

            var (isFinished, items) = await _pgcProvider.GetPgcIndexResultAsync(_type, queryPrameters);
            _isFinished = isFinished;
            foreach (var item in items)
            {
                var seasonVM = Locator.Instance.GetService<ISeasonItemViewModel>();
                seasonVM.InjectData(item);
                Items.Add(seasonVM);
            }

            IsEmpty = Items.Count == 0;
        }
    }
}
