﻿// Copyright (c) Richasy. All rights reserved.

using System;
using Bili.Models.App.Args;
using Bili.Models.Data.Player;
using Bili.Models.Enums;
using Bili.Toolkit.Interfaces;
using CommunityToolkit.Mvvm.Input;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;

namespace Bili.ViewModels.Uwp.Core
{
    /// <summary>
    /// 原生播放器视图模型.
    /// </summary>
    public sealed partial class NativePlayerViewModel
    {
        private readonly IFileToolkit _fileToolkit;
        private readonly IResourceToolkit _resourceToolkit;
        private readonly CoreDispatcher _dispatcher;

        private SegmentInformation _video;
        private SegmentInformation _audio;
        private MediaPlayer _videoPlayer;
        private MediaSource _videoSource;
        private MediaPlaybackItem _videoPlaybackItem;
        private HttpRandomAccessStream _liveStream;
        private bool _shouldPreventSkip;

        /// <inheritdoc/>
        public event EventHandler MediaOpened;

        /// <inheritdoc/>
        public event EventHandler MediaEnded;

        /// <inheritdoc/>
        public event EventHandler<MediaStateChangedEventArgs> StateChanged;

        /// <inheritdoc/>
        public event EventHandler<MediaPositionChangedEventArgs> PositionChanged;

        /// <inheritdoc/>
        public event EventHandler<object> MediaPlayerChanged;

        /// <inheritdoc/>
        public IRelayCommand ClearCommand { get; }

        /// <inheritdoc/>
        public TimeSpan Position => _videoPlayer?.PlaybackSession?.Position ?? TimeSpan.Zero;

        /// <inheritdoc/>
        public TimeSpan Duration => _videoPlayer?.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;

        /// <inheritdoc/>
        public double Volume => (_videoPlayer?.Volume ?? 1d) * 100;

        /// <inheritdoc/>
        public double PlayRate => _videoPlayer?.PlaybackSession?.PlaybackRate ?? -1d;

        /// <inheritdoc/>
        public PlayerStatus Status { get; set; }

        /// <inheritdoc/>
        public bool IsLoop => _videoPlayer?.IsLoopingEnabled ?? false;

        /// <inheritdoc/>
        public bool IsPlayerReady => _videoPlayer != null && _videoPlayer.PlaybackSession != null;
    }
}
