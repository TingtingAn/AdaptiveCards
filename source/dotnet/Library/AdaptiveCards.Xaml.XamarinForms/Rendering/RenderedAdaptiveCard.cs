// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
#if WPF
using System.Windows;
#elif XAMARIN
using Xamarin.Forms;
using FrameworkElement = Xamarin.Forms.View;
using Button = AdaptiveCards.Rendering.ContentButton;
#endif

namespace AdaptiveCards.Rendering.XamarinForms
{
    public class RenderedAdaptiveCard : RenderedAdaptiveCardBase
    {
        public RenderedAdaptiveCard(
            FrameworkElement frameworkElement,
            AdaptiveCard originatingCard,
            IList<AdaptiveWarning> warnings,
            IDictionary<string, Func<string>> inputBindings)
            : base(originatingCard, warnings)
        {
            FrameworkElement = frameworkElement;
            UserInputs = new RenderedAdaptiveCardInputs(inputBindings);
        }

        /// <summary>
        /// The rendered card
        /// </summary>
        public FrameworkElement FrameworkElement { get; }

        /// <summary>
        /// Event handler for when user invokes an action.
        /// </summary>
        public event TypedEventHandler<RenderedAdaptiveCard, AdaptiveActionEventArgs> OnAction;

        internal void InvokeOnAction(AdaptiveActionEventArgs args)
        {
            OnAction?.Invoke(this, args);
        }

        /// <summary>
        /// Event handler for when user clicks a media.
        /// </summary>
        public event TypedEventHandler<RenderedAdaptiveCard, AdaptiveMediaEventArgs> OnMediaClicked;

        internal void InvokeOnMediaClick(AdaptiveMediaEventArgs args)
        {
            OnMediaClicked?.Invoke(this, args);
        }

        /// <summary>
        /// Event handler for when user clicks a media.
        /// </summary>
        public event EventHandler<View> OnImageLoaded;

        internal void InvokeOnImageLoaded(View args)
        {
            OnImageLoaded?.Invoke(this, args);
        }
    }
}
