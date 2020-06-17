// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if WPF
using System.Windows.Controls;
#elif XAMARIN
using Xamarin.Forms;
using FrameworkElement = Xamarin.Forms.View;
#endif

namespace AdaptiveCards.Rendering.XamarinForms
{
    /// <summary>
    /// Class to add to all rendered items so ToggleVisibility can hide the separators or deduct the spacing from the margin
    /// </summary>
    public class TagContent
    {

        public TagContent(Grid separator, Grid elementContainer)
        {
            Separator = separator;
            ParentContainerElement = elementContainer;
        }

        public Grid Separator { get; set; } = null;

        /// <summary>
        /// Grid that contains the rendered element
        /// </summary>
        public Grid ParentContainerElement { get; set; }

        /// <summary>
        /// Column Definition for the rendered column in the columnset
        /// </summary>
        public ColumnDefinition ColumnDefinition { get; set; } = null;

        /// <summary>
        /// Row Definition for the rendered element in the container
        /// </summary>
        public RowDefinition RowDefinition { get; set; } = null;

        public int ViewIndex { get; set; }
    }
}
