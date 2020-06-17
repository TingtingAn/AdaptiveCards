// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Windows;
#if WPF
using System.Windows.Controls;
using System.Windows.Media;
#elif XAMARIN
using FrameworkElement = Xamarin.Forms.View;
using Xamarin.Forms;
using Button = AdaptiveCards.Rendering.ContentButton;
#endif

namespace AdaptiveCards.Rendering.XamarinForms
{
    public static class AdaptiveActionRenderer
    {
        public static FrameworkElement Render(AdaptiveAction action, AdaptiveRenderContext context)
        {
            if (context.Config.SupportsInteractivity && context.ActionHandlers.IsSupported(action.GetType()))
            {
                var uiButton = CreateActionButton(action, context);
                uiButton.Click += (sender, e) =>
                {
                    context.InvokeAction(uiButton, new AdaptiveActionEventArgs(action));

                    // Prevent nested events from triggering
                    // e.Handled = true;
                };
                return uiButton;
            }
            return null;
        }

        public static Button CreateActionButton(AdaptiveAction action, AdaptiveRenderContext context)
        {
            var uiButton = new Button
            {
                Style = context.GetStyle($"Adaptive.{action.Type}"),
            };
            uiButton.BackgroundColor = Color.LightGray;
            uiButton.HorizontalOptions = LayoutOptions.Start;

            if (!String.IsNullOrWhiteSpace(action.Style))
            {
                Style style = context.GetStyle($"Adaptive.Action.{action.Style}");

                if (style == null && String.Equals(action.Style, "positive", StringComparison.OrdinalIgnoreCase))
                {
                    style = context.GetStyle("PositiveActionDefaultStyle");
                }
                else if (style == null && String.Equals(action.Style, "destructive", StringComparison.OrdinalIgnoreCase))
                {
                    style = context.GetStyle("DestructiveActionDefaultStyle");
                }

                uiButton.Style = style;
            }

#if WPF
            var contentStackPanel = new StackPanel();
#elif XAMARIN
            var contentStackPanel = new StackLayout();
#endif

            if (!context.IsRenderingSelectAction)
            {
                // Only apply padding for normal card actions
                //uiButton.Padding = new Thickness(6, 4, 6, 4);
                uiButton.Padding = new Thickness(15, 10, 15, 10);
                uiButton.CornerRadius = 5;
            }
            else
            {
                // Remove any extra spacing for selectAction
                uiButton.Padding = new Thickness(15,10,15,10);
                uiButton.CornerRadius = 5;
                contentStackPanel.Padding = new Thickness(15, 10, 15, 10);
                //contentStackPanel.Margin = new Thickness(0, 0, 0, 0);
            }
            uiButton.Content = contentStackPanel;
            FrameworkElement uiIcon = null;

            var uiTitle = new TextBlock
            {
                Text = action.Title,
                HorizontalTextAlignment= TextAlignment.Center,
                FontSize = context.Config.GetFontSize(AdaptiveFontType.Default, AdaptiveTextSize.Default),
                Style = context.GetStyle($"Adaptive.Action.Title")
            };

            if (action.IconUrl != null)
            {
                var actionsConfig = context.Config.Actions;

                var image = new AdaptiveImage(action.IconUrl)
                {
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center
                };
                uiIcon = AdaptiveImageRenderer.Render(image, context);
#if WPF
                if (actionsConfig.IconPlacement == IconPlacement.AboveTitle)
                {
                    contentStackPanel.Orientation = Orientation.Vertical;
                    uiIcon.Height = (double)actionsConfig.IconSize;
                }
                else
                {
                    contentStackPanel.Orientation = Orientation.Horizontal;
                    //Size the image to the textblock, wait until layout is complete (loaded event)
                    uiIcon.Loaded += (sender, e) =>
                    {
                        uiIcon.Height = uiTitle.ActualHeight;
                    };
                }
#endif
                contentStackPanel.Children.Add(uiIcon);

                // Add spacing for the icon for horizontal actions
                if (actionsConfig.IconPlacement == IconPlacement.LeftOfTitle)
                {
                    int spacing = context.Config.GetSpacing(AdaptiveSpacing.Default);
                    var uiSep = new Grid
                    {
                        Style = context.GetStyle($"Adaptive.VerticalSeparator"),
#if WPF
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Width = spacing,
#endif
                    };
                    contentStackPanel.Children.Add(uiSep);
                }
            }

            contentStackPanel.Children.Add(uiTitle);
            return uiButton;
        }
    }
}
