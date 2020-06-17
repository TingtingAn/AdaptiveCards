// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using FrameworkElement = Xamarin.Forms.View;
using Xamarin.Forms;
using Xamarin.Forms.Markup;

namespace AdaptiveCards.Rendering.XamarinForms
{
    public static class AdaptiveMediaRenderer
    {
        private enum MediaState
        {
            NotStarted = 0,
            IsPlaying,
            IsPaused,
        }

        #region Appearance Config

        // Element height and margin
        // NOTE: Child height + child margin * 2 = panel height (50)
        private static readonly int _childHeight = 40;
        private static readonly int _childMargin = 5;
        private static readonly int _panelHeight = _childHeight + _childMargin * 2;
        private static readonly Thickness _marginThickness = new Thickness(_childMargin, _childMargin, _childMargin, _childMargin);

        // Use contrasting colors for foreground and background
        private static readonly Color _controlForegroundColor = Color.White;
        private static readonly Color _controlBackgroundColor = Color.Gray.MultiplyAlpha(0.5f);

        private static readonly string _symbolFontFamily = "Segoe UI Symbol";

        #endregion

        public static FrameworkElement Render(AdaptiveMedia media, AdaptiveRenderContext context)
        {
            // If host doesn't support interactivity or no media source is provided
            // just return the poster image if present
            if (!context.Config.SupportsInteractivity || media.Sources.Count == 0)
            {
                return GetPosterImage(media, context);
            }

            AdaptiveMediaSource mediaSource = GetMediaSource(media, context);
            if (mediaSource == null)
            {
                return null;
            }

            // Main element to return
            var uiMedia = new Grid();

            #region Thumbnail button

            var mediaConfig = context.Config.Media;

            var uiThumbnailButton = new Grid
            {
                //Name = "thumbnailButton",
            };

            /* Poster Image */

            // A poster container is necessary to handle background color and opacity mask
            // in case poster image is not found or does not exist
            var uiPosterContainer = new Grid()
            {
                BackgroundColor = _controlBackgroundColor,
            };

            Image uiPosterImage = GetPosterImage(media, context);
            if (uiPosterImage != null)
            {
                uiPosterContainer.Children.Add(uiPosterImage);
            }

            uiThumbnailButton.Children.Add(uiPosterContainer);

            // Play button
            var uiPlayButton = RenderThumbnailPlayButton(context);
            uiThumbnailButton.Children.Add(uiPlayButton);

            // Mouse hover handlers to signify playable media element
                    //uiThumbnailButton.MouseEnter += (sender, e) =>
                    //{
                    //    uiPosterContainer.OpacityMask = _controlBackgroundColor;
                    //};
                    //uiThumbnailButton.MouseLeave += (sender, e) =>
                    //{
                    //    uiPosterContainer.OpacityMask = null;
                    //};

            #endregion

            uiMedia.Children.Add(uiThumbnailButton);

            // Inline playback is possible only when inline playback is allowed by the host and the chosen media source is not https
            bool isInlinePlaybackPossible = mediaConfig.AllowInlinePlayback && context.Config.ResolveFinalAbsoluteUri(mediaSource.Url).Scheme != "https";

            FrameworkElement uiMediaPlayer = null;
            if (isInlinePlaybackPossible)
            {
                // Media player is only created if inline playback is allowed
                uiMediaPlayer = RenderMediaPlayer(context, mediaSource, uiMedia);
                uiMediaPlayer.IsVisible = false;

                uiMedia.Children.Add(uiMediaPlayer);
            }

            // Play the media
            
            uiThumbnailButton.AddTapGesture(gesture =>
            {
                if (isInlinePlaybackPossible)
                {
                    if (IsAudio(mediaSource) && uiPosterImage != null)
                    {
                        // If media is audio, keep only the poster image (if present)
                        // and disable the thumbnail button to prevent further clicks
                        uiPlayButton.IsVisible = false;
                        uiThumbnailButton.IsEnabled = false;
                    }
                    else
                    {
                        // Otherwise, collapse all the thumbnail button
                        uiThumbnailButton.IsVisible = false;
                    }

                    // Show the media player to start
                    uiMediaPlayer.IsVisible =true;
                }
                // Raise an event to send the media to host
                else
                {
                    context.ClickMedia(uiPosterContainer, new AdaptiveMediaEventArgs(media));
                    if (uiMediaPlayer == null)
                    {
                        uiMediaPlayer = RenderMediaPlayer(context, mediaSource, uiMedia);
                        uiMedia.Children.Add(uiMediaPlayer);
                    }
                    // Prevent nested events from triggering
                    //e.Handled = true;
                }
            });
            return uiMedia;
        }

        private static void AddTapGesture(this View view,Action<GestureRecognizer> action)
        {
            var tapgesture = new TapGestureRecognizer();
            tapgesture.Tapped += (sender, e) =>
            {
                action?.Invoke(null);
            };
            view.GestureRecognizers.Add(tapgesture);
        }

        private static FrameworkElement RenderThumbnailPlayButton(AdaptiveRenderContext context)
        {
            var playButtonSize = 100;

            // Wrap in a Viewbox to control width, height, and aspect ratio
            var uiPlayButton = new ContentView()
            {
                WidthRequest = playButtonSize,
                HeightRequest = playButtonSize,
                //Stretch = Stretch.Fill,
                Margin = _marginThickness,
            };

            MediaConfig mediaConfig = context.Config.Media;
            if (!string.IsNullOrEmpty(mediaConfig.PlayButton)
                 && context.Config.ResolveFinalAbsoluteUri(mediaConfig.PlayButton) != null)
            {
                // Try to use provided image from host config
                var content = new Image()
                {
                    HeightRequest = playButtonSize,
                };
                content.SetImageSource(mediaConfig.PlayButton, context);

                uiPlayButton.Content = content;
            }
            else
            {
                // Otherwise, use the default play symbol
                var content = new TextBlock()
                {
                    Text = "⏵",
                    FontFamily = _symbolFontFamily,
                    TextColor = _controlForegroundColor,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                };

                uiPlayButton.Content = content;
            }

            return uiPlayButton;
        }

        /** The media player contains the following elements:
         *  - Media element
         *  - Control panel:
         *      - Timeline panel to keep track of media progress and provide seek to functionality
         *      - Playback panel:
         *          - Pause button
         *          - Resume button
         *          - Replay button
         *          - Volume controls:
         *              - Mute/unmute
         *              - Volume slider
         */
        private static FrameworkElement RenderMediaPlayer(AdaptiveRenderContext context, AdaptiveMediaSource mediaSource, FrameworkElement uiMedia)
        {
            var uiMediaPlayer = new Grid();

            var mediaElement = new MediaElement()
            {
            };
            mediaElement.Source = mediaSource.Url;
            mediaElement.AutoPlay = true;
            mediaElement.ShowsPlaybackControls = true;
            uiMediaPlayer.Children.Add(mediaElement);

            // Add some height to keep the controls (timeline panel + playback panel)
            if (!IsAudio(mediaSource))
            {
                //uiMediaPlayer.MinHeight = _panelHeight * 2 + _childMargin * 4;
                uiMediaPlayer.MinHeight(_panelHeight * 2 + _childMargin * 4);
            }
            return uiMediaPlayer;

            var uiControlPanel = new StackLayout()
            {
                VerticalOptions = LayoutOptions.End,
                BackgroundColor = _controlBackgroundColor,
            };

            #region Timeline Panel

            // Using Grid to stretch the timeline slider
            var uiTimelinePanel = new Grid()
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = _panelHeight,
            };

            TextBlock uiCurrentTime = new TextBlock()
            {
                Text = "00:00:00",
                TextColor = _controlForegroundColor,
                VerticalOptions = LayoutOptions.Center,
                Margin = _marginThickness,
            };
            uiTimelinePanel.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(20, GridUnitType.Auto),
            });
            Grid.SetColumn(uiCurrentTime, 0);
            uiTimelinePanel.Children.Add(uiCurrentTime);

            Slider uiTimelineSlider = new Slider()
            {
                Margin = _marginThickness,
                IsEnabled = false,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center,
            };
            uiTimelinePanel.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(20, GridUnitType.Star),
            });
            Grid.SetColumn(uiTimelineSlider, 1);
            uiTimelinePanel.Children.Add(uiTimelineSlider);

            TextBlock uiMaxTime = new TextBlock()
            {
                Text = "00:00:00",
                TextColor = _controlForegroundColor,
                VerticalOptions = LayoutOptions.Center,
                Margin = _marginThickness,
            };
            uiTimelinePanel.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(20, GridUnitType.Auto),
            });
            Grid.SetColumn(uiMaxTime, 2);
            uiTimelinePanel.Children.Add(uiMaxTime);

            #endregion

            uiControlPanel.Children.Add(uiTimelinePanel);

            #region Playback + Volume Panel

            var uiPlaybackVolumePanel = new Grid()
            {
                HeightRequest = _panelHeight,
            };

            #region Create Playback Control Container

            var uiPlaybackControlContainer = new StackLayout()
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                Orientation =  StackOrientation.Horizontal,
                HeightRequest = _panelHeight,
            };

            // Play trigger attached with the thumbnail button
            var playTrigger = new EventTrigger()
            {
                //SourceName = "thumbnailButton",
            };

            //var mediaTimeline = new MediaTimeline()
            //{
            //    // This URI was previously confirmed to be valid
            //    Source = context.Config.ResolveFinalAbsoluteUri(mediaSource.Url),
            //};
            //Storyboard.SetTarget(mediaTimeline, mediaElement);

            //var storyboard = new Storyboard()
            //{
            //    SlipBehavior = SlipBehavior.Slip,
            //};
            //storyboard.Children.Add(mediaTimeline);
            //var beginStoryboard = new BeginStoryboard()
            //{
            //    // An arbitrary name is necessary for the storyboard to work correctly
            //    Name = "beginStoryboard",
            //    Storyboard = storyboard,
            //};
            //playTrigger.Actions.Add(beginStoryboard);

            // The play trigger needs to be added to uiMedia since
            // uiThumbnailButton is inside uiMedia
            uiMedia.Triggers.Add(playTrigger);

            // Buffering signal
            var uiBuffering = new TextBlock()
            {
                Text = "⏳ Buffering...",
                TextColor = _controlForegroundColor,
                VerticalOptions = LayoutOptions.Center,
                Margin = _marginThickness,
            };
            uiPlaybackControlContainer.Children.Add(uiBuffering);

            // Pause button
            var uiPauseButton = RenderPlaybackButton("⏸");
            uiPlaybackControlContainer.Children.Add(uiPauseButton);

            // Resume button
            var uiResumeButton = RenderPlaybackButton("⏵");
            uiPlaybackControlContainer.Children.Add(uiResumeButton);

            // Replay button
            var uiReplayButton = RenderPlaybackButton("🔄");
            uiPlaybackControlContainer.Children.Add(uiReplayButton);

            // Click events
            MediaState currentMediaState = MediaState.NotStarted;
            //uiPauseButton.MouseUp += (sender, e) =>
            //{
            //    storyboard.Pause(uiMedia);
            //    currentMediaState = MediaState.IsPaused;
            //    HandlePlaybackButtonVisibility(currentMediaState, uiPauseButton, uiResumeButton, uiReplayButton);
            //};
            //uiResumeButton.MouseUp += (sender, e) =>
            //{
            //    storyboard.Resume(uiMedia);
            //    currentMediaState = MediaState.IsPlaying;
            //    HandlePlaybackButtonVisibility(currentMediaState, uiPauseButton, uiResumeButton, uiReplayButton);
            //};

            #endregion

            // Add to control panel
            uiPlaybackVolumePanel.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(20, GridUnitType.Star),
            });
            Grid.SetColumn(uiPlaybackControlContainer, 0);
            uiPlaybackVolumePanel.Children.Add(uiPlaybackControlContainer);

            #region Create Volume Control Container

            var uiVolumeControlContainer = new Grid()
            {
                HeightRequest = _panelHeight,
                VerticalOptions = LayoutOptions.Center,
            };

            // 2 overlapping buttons to alternate between mute/unmute states
            var uiVolumeMuteButton = new TextBlock()
            {
                Text = "🔊",
                FontFamily = _symbolFontFamily,
                FontSize = _childHeight,
                TextColor = _controlForegroundColor,
                Margin = _marginThickness,
                VerticalOptions = LayoutOptions.Center,
            };
            uiVolumeControlContainer.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(20, GridUnitType.Auto),
            });
            Grid.SetColumn(uiVolumeMuteButton, 0);
            uiVolumeControlContainer.Children.Add(uiVolumeMuteButton);

            var uiVolumeUnmuteButton = new TextBlock()
            {
                Text = "🔇",
                FontFamily = _symbolFontFamily,
                FontSize = _childHeight,
                TextColor = _controlForegroundColor,
                Margin = _marginThickness,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false,
            };
            uiVolumeControlContainer.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(20, GridUnitType.Auto),
            });
            Grid.SetColumn(uiVolumeUnmuteButton, 0);
            uiVolumeControlContainer.Children.Add(uiVolumeUnmuteButton);

            Slider uiVolumeSlider = new Slider()
            {
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 100,

                Minimum = 0,
                Maximum = 1,
                Value = 0.5,
            };
            uiVolumeControlContainer.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(20, GridUnitType.Auto),
            });
            Grid.SetColumn(uiVolumeSlider, 1);
            uiVolumeControlContainer.Children.Add(uiVolumeSlider);

            // Initialize media volume to 0.5
            mediaElement.Volume = uiVolumeSlider.Value;


            uiVolumeMuteButton.AddTapGesture((gesture) =>
            {
                uiVolumeMuteButton.IsVisible = false;
                uiVolumeUnmuteButton.IsVisible = true;
                mediaElement.Volume = 0;
            });
            uiVolumeUnmuteButton.AddTapGesture((gesture)=> {
                uiVolumeUnmuteButton.IsVisible = false;
                uiVolumeMuteButton.IsVisible = true;
                mediaElement.Volume = uiVolumeSlider.Value;
            });
            uiVolumeSlider.ValueChanged += (sender,e)=> {
                uiVolumeUnmuteButton.IsVisible = false;
                uiVolumeMuteButton.IsVisible = true;
                mediaElement.Volume = uiVolumeSlider.Value;
            };

            #endregion

            // Add to control panel
            uiPlaybackVolumePanel.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(20, GridUnitType.Auto),
            });
            Grid.SetColumn(uiVolumeControlContainer, 1);
            uiPlaybackVolumePanel.Children.Add(uiVolumeControlContainer);

            #endregion

            uiControlPanel.Children.Add(uiPlaybackVolumePanel);

            uiMediaPlayer.Children.Add(uiControlPanel);

            #region Other events

            void mediaStarted(object sender, EventArgs e)
            {
                // Playback button visibility
                currentMediaState = MediaState.IsPlaying;
                uiBuffering.IsVisible = false;
                HandlePlaybackButtonVisibility(currentMediaState, uiPauseButton, uiResumeButton, uiReplayButton);

                // Control panel visibility
                if (!IsAudio(mediaSource))
                {
                    // Hide when the video starts playing
                    uiControlPanel.IsVisible = false;

                    // Assign mouse hover events to avoid blocking the video
                    //uiMediaPlayer.MouseEnter += showControlPanel;
                    //uiMediaPlayer.MouseLeave += collapseControlPanel;
                }
            }

            mediaElement.MediaOpened += mediaStarted;
            uiReplayButton.AddTapGesture(gesture=>
            {
                // Seek to beginning
                //storyboard.Seek(uiMedia, new TimeSpan(0, 0, 0, 0, 0), TimeSeekOrigin.BeginTime);
                mediaElement.Position = TimeSpan.Zero;
                // And start the media as usual
                mediaStarted(null, null);
            });

            // Timeline slider events
            mediaElement.MediaOpened += (sender, e) =>
            {
                //uiTimelineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
                uiTimelineSlider.Maximum = mediaElement.Duration.Value.TotalMilliseconds;
                uiTimelineSlider.IsEnabled = true;

                uiMaxTime.Text = mediaElement.Duration.Value.ToString(@"hh\:mm\:ss");
            };

            mediaElement.MediaEnded += (sender, e) =>
            {
                // Playback button visibility
                currentMediaState = MediaState.NotStarted;
                HandlePlaybackButtonVisibility(currentMediaState, uiPauseButton, uiResumeButton, uiReplayButton);

                // Control panel visibility
                if (!IsAudio(mediaSource))
                {
                    // Show when the video is complete
                    uiControlPanel.IsVisible = true;

                    // Remove mouse hover events to always show controls
                    //uiMediaPlayer.MouseEnter -= showControlPanel;
                    //uiMediaPlayer.MouseLeave -= collapseControlPanel;
                }
            };
            //storyboard.CurrentTimeInvalidated += (sender, e) => {
            //    uiTimelineSlider.Value = mediaElement.Position.TotalMilliseconds;

            //    uiCurrentTime.Text = mediaElement.Position.ToString(@"hh\:mm\:ss");
            //};
            mediaElement.PositionRequested += (sender, e) =>
            {
                uiTimelineSlider.Value = mediaElement.Position.TotalMilliseconds;
                uiCurrentTime.Text = mediaElement.Position.ToString(@"hh\:mm\:ss");
            };

            // Thumb events
            //uiTimelineSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler((sender, e) =>
            //{
            //    storyboard.Pause(uiMedia);
            //}));
            uiTimelineSlider.DragStarted += (sender, e) =>
            {
                mediaElement.Pause();
            };
            uiTimelineSlider.DragCompleted += (sender, e) =>
            {
                int sliderValue = (int)uiTimelineSlider.Value;
                //storyboard.Seek(uiMedia, new TimeSpan(0, 0, 0, 0, sliderValue), TimeSeekOrigin.BeginTime);
                mediaElement.Position = TimeSpan.FromSeconds(sliderValue);

                // Only resume playing if it's in playing mode
                if (currentMediaState == MediaState.IsPlaying)
                {
                    //storyboard.Resume(uiMedia);
                    mediaElement.Play();
                }
            };
            //uiTimelineSlider.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler((sender, e) =>
            //{
                
            //}));
            uiTimelineSlider.ValueChanged += (sender, e) =>
            {
                int sliderValue = (int)uiTimelineSlider.Value;

                TimeSpan selectedTimeSpan = new TimeSpan(0, 0, 0, 0, sliderValue);

                uiCurrentTime.Text = selectedTimeSpan.ToString(@"hh\:mm\:ss");
            };
            //uiTimelineSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((sender, e) =>
            //{
               
            //}));

            #endregion

            return uiMediaPlayer;
        }

        /** Helper function to call async function from context */
        private static void SetImageSource(this Image image, string urlString, AdaptiveRenderContext context)
        {
            image.Source = urlString;
        }

        /** Get poster image from either card payload or host config */
        private static Image GetPosterImage(AdaptiveMedia media, AdaptiveRenderContext context)
        {
            Image uiPosterImage = null;
            if (!string.IsNullOrEmpty(media.Poster) && context.Config.ResolveFinalAbsoluteUri(media.Poster) != null)
            {
                // Use the provided poster
                uiPosterImage = new Image();
                uiPosterImage.SetImageSource(media.Poster, context);
            }
            else if (!string.IsNullOrEmpty(context.Config.Media.DefaultPoster)
                 && context.Config.ResolveFinalAbsoluteUri(context.Config.Media.DefaultPoster) != null)
            {
                // Use the default poster from host
                uiPosterImage = new Image();
                uiPosterImage.SetImageSource(context.Config.Media.DefaultPoster, context);
            }

            return uiPosterImage;
        }

        /** Simple template for playback buttons */
        private static FrameworkElement RenderPlaybackButton(string text)
        {
            return new ContentView()
            {
                WidthRequest = _childHeight,
                HeightRequest = _childHeight,
                Margin = _marginThickness,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false,
                Content = new TextBlock()
                {
                    Text = text,
                    FontFamily = _symbolFontFamily,
                    TextColor = _controlForegroundColor,
                }
            };
        }

        /** Handle visibility of playback buttons based on the current media state
         *  - If it's playing, user can only pause
         *  - If it's paused, user can only resume
         *  - If it's complete, user can only replay
         */
        private static void HandlePlaybackButtonVisibility(MediaState currentMediaState,
            FrameworkElement pauseButton, FrameworkElement resumeButton, FrameworkElement replayButton)
        {
            pauseButton.IsVisible = false;
            resumeButton.IsVisible = false;
            replayButton.IsVisible = false;

            if (currentMediaState == MediaState.IsPlaying)
            {
                pauseButton.IsVisible = true;
            }
            else if (currentMediaState == MediaState.IsPaused)
            {
                resumeButton.IsVisible = true;
            }
            else
            {
                // Video is complete
                replayButton.IsVisible = true;
            }
        }

        private static List<string> _supportedMimeTypes = new List<string>
        {
            "video/mp4",
            "audio/mp4",
            "audio/mpeg"
        };

        private static List<string> _supportedAudioMimeTypes = new List<string>
        {
            "audio/mp4",
            "audio/mpeg"
        };

        /** Get the first media URI with a supported mime type */
        private static AdaptiveMediaSource GetMediaSource(AdaptiveMedia media, AdaptiveRenderContext context)
        {
            // Check if sources contain an invalid mix of MIME types (audio and video)
            bool? isLastMediaSourceAudio = null;
            foreach (var source in media.Sources)
            {
                if (!isLastMediaSourceAudio.HasValue)
                {
                    isLastMediaSourceAudio = IsAudio(source);
                }
                else
                {
                    if (IsAudio(source) != isLastMediaSourceAudio.Value)
                    {
                        // If there is one pair of sources with different MIME types,
                        // it's an invalid mix and a warning should be logged
                        context.Warnings.Add(new AdaptiveWarning(-1, "A Media element contains an invalid mix of MIME type"));
                        return null;
                    }

                    isLastMediaSourceAudio = IsAudio(source);
                }
            }

            // Return the first supported source with not-null URI
            bool isAllHttps = true;
            AdaptiveMediaSource httpsSource = null;
            foreach (var source in media.Sources)
            {
                if (_supportedMimeTypes.Contains(source.MimeType))
                {
                    Uri finalMediaUri = context.Config.ResolveFinalAbsoluteUri(source.Url);
                    if (finalMediaUri != null)
                    {
                        // Since https is not supported by WPF,
                        // try to use non-https sources first
                        if (finalMediaUri.Scheme != "https")
                        {
                            isAllHttps = false;
                            return source;
                        }
                        else if (httpsSource == null)
                        {
                            httpsSource = source;
                        }
                    }
                }
            }

            // If all sources are https, log a warning and return the first one
            if (isAllHttps)
            {
                context.Warnings.Add(new AdaptiveWarning(-1, "All sources have unsupported https scheme. The host would be responsible for playing the media."));
                return httpsSource;
            }

            // No valid source is found
            context.Warnings.Add(new AdaptiveWarning(-1, "A Media element does not have any valid source"));
            return null;
        }

        private static bool IsAudio(AdaptiveMediaSource mediaSource)
        {
            return _supportedAudioMimeTypes.Contains(mediaSource.MimeType);
        }
    }
}
