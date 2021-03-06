// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.MarkedNet;
#if WPF
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
#elif XAMARIN
using Xamarin.Forms;
using FrameworkElement = Xamarin.Forms.View;
#endif

namespace AdaptiveCards.Rendering.XamarinForms
{
    public static class AdaptiveTextBlockRenderer
    {
        public static FrameworkElement Render(AdaptiveTextBlock textBlock, AdaptiveRenderContext context)
        {
            var uiTextBlock = CreateControl(textBlock, context);

            uiTextBlock.SetColor(textBlock.Color, textBlock.IsSubtle, context);

            if (textBlock.MaxWidth > 0)
            {
#if WPF
                uiTextBlock.MaxWidth = textBlock.MaxWidth;
#endif
            }

            if (textBlock.MaxLines > 0)
            {
                var uiGrid = new Grid();
                uiGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

#if WPF
                // create hidden textBlock with appropriate linebreaks that we can use to measure the ActualHeight
                // using same style, fontWeight settings as original textblock
                var measureBlock = new TextBlock()
                {
                    Style = uiTextBlock.Style,
                    FontWeight = uiTextBlock.FontWeight,
                    FontSize = uiTextBlock.FontSize,
                    Visibility = Visibility.Hidden,
                    TextWrapping = TextWrapping.NoWrap,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    DataContext = textBlock.MaxLines
                };

                measureBlock.Inlines.Add(uiTextBlock.Text);

                // bind the real textBlock's Height => MeasureBlock.ActualHeight
                uiTextBlock.SetBinding(FrameworkElement.MaxHeightProperty, new Binding()
                {
                    Path = new PropertyPath("ActualHeight"),
                    Source = measureBlock,
                    Mode = BindingMode.OneWay,
                    Converter = new MultiplyConverter(textBlock.MaxLines)
                });

                // Add both to a grid so they go as a unit
                uiGrid.Children.Add(measureBlock);
#elif XAMARIN
                // TODO 
#endif

                uiGrid.Children.Add(uiTextBlock);
                return uiGrid;

            }

            return uiTextBlock;
        }

        private static TextBlock CreateControl(AdaptiveTextBlock textBlock, AdaptiveRenderContext context)
        {
            Marked marked = new Marked();
            marked.Options.Renderer = new AdaptiveXamlMarkdownRenderer();
            marked.Options.Mangle = false;
            marked.Options.Sanitize = true;

            string text = RendererUtilities.ApplyTextFunctions(textBlock.Text, context.Lang);
            // uiTextBlock.Text = textBlock.Text;
            string xaml = $"<TextBlock  xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{marked.Parse(text)}</TextBlock>";
            StringReader stringReader = new StringReader(xaml);

            XmlReader xmlReader = XmlReader.Create(stringReader);
#if WPF
            var uiTextBlock = (System.Windows.Controls.TextBlock)XamlReader.Load(xmlReader);
            uiTextBlock.Style = context.GetStyle($"Adaptive.{textBlock.Type}");

            uiTextBlock.TextWrapping = TextWrapping.NoWrap;

            uiTextBlock.FontFamily = new FontFamily(RendererUtil.GetFontFamilyFromList(context.Config.GetFontFamily(textBlock.FontType)));
            uiTextBlock.FontWeight = FontWeight.FromOpenTypeWeight(context.Config.GetFontWeight(textBlock.FontType, textBlock.Weight));
            uiTextBlock.FontSize = context.Config.GetFontSize(textBlock.FontType, textBlock.Size);

            uiTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;

            if (textBlock.Italic)
            {
                uiTextBlock.FontStyle = FontStyles.Italic;
            }

            if (textBlock.Strikethrough)
            {
                uiTextBlock.TextDecorations = TextDecorations.Strikethrough;
            }

            if (textBlock.HorizontalAlignment != AdaptiveHorizontalAlignment.Left)
            {
                System.Windows.TextAlignment alignment;
                if (Enum.TryParse<System.Windows.TextAlignment>(textBlock.HorizontalAlignment.ToString(), out alignment))
                    uiTextBlock.TextAlignment = alignment;
            }

            if (textBlock.Wrap)
                uiTextBlock.TextWrapping = TextWrapping.Wrap;

            return uiTextBlock;
#elif XAMARIN
            var uiTextBlock = AdaptiveCards.Rendering.XamlTextBlock.CreateControl(textBlock, context);
            uiTextBlock.FontSize = context.Config.GetFontSize(textBlock.FontType, textBlock.Size);

            return uiTextBlock;
#endif
        }

        private class MultiplyConverter : IValueConverter
        {
            private int multiplier;

            public MultiplyConverter(int multiplier)
            {
                this.multiplier = multiplier;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (double)value * this.multiplier;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (double)value * this.multiplier;
            }
        }
    }
}
