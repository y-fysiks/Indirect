// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.ApplicationModel;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// ReSharper disable CheckNamespace
namespace Indirect.Controls
{
    /// <summary>
    /// Panel that allows for a Master/Details pattern.
    /// </summary>
    [TemplatePart(Name = PartDetailsPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartDetailsPanel, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartMainShadow, Type = typeof(ThemeShadow))]
    [TemplatePart(Name = PartMasterPanel, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartDetailsPanel, Type = typeof(FrameworkElement))]
    [TemplateVisualState(Name = NoSelectionNarrowState, GroupName = SelectionStates)]
    [TemplateVisualState(Name = NoSelectionWideState, GroupName = SelectionStates)]
    [TemplateVisualState(Name = HasSelectionWideState, GroupName = SelectionStates)]
    [TemplateVisualState(Name = HasSelectionNarrowState, GroupName = SelectionStates)]
    [TemplateVisualState(Name = NarrowState, GroupName = WidthStates)]
    [TemplateVisualState(Name = IntermediateState, GroupName = WidthStates)]
    [TemplateVisualState(Name = WideState, GroupName = WidthStates)]
    public partial class ExtendedMasterDetailsView : ItemsControl
    {
        private const string PartDetailsPresenter = "DetailsPresenter";
        private const string PartDetailsPanel = "DetailsPanel";
        private const string PartMasterPanel = "MasterPanel";
        private const string PartBackButton = "MasterDetailsBackButton";
        private const string PartHeaderContentPresenter = "HeaderContentPresenter";
        private const string NarrowState = "NarrowState";
        private const string WideState = "WideState";
        private const string IntermediateState = "IntermediateState";
        private const string WidthStates = "WidthStates";
        private const string PartMainShadow = "MainShadow";
        private const string SelectionStates = "SelectionStates";
        private const string HasSelectionNarrowState = "HasSelectionNarrow";
        private const string HasSelectionWideState = "HasSelectionWide";
        private const string NoSelectionNarrowState = "NoSelectionNarrow";
        private const string NoSelectionWideState = "NoSelectionWide";

        private AppViewBackButtonVisibility? _previousSystemBackButtonVisibility;
        private bool _previousNavigationViewBackEnabled;

        // Int used because the underlying type is an enum, but we don't have access to the enum
        private int _previousNavigationViewBackVisibilty;
        private ContentPresenter _detailsPresenter;
        private VisualStateGroup _selectionStateGroup;
        private Button _inlineBackButton;
        private object _navigationView;
        private Frame _frame;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedMasterDetailsView"/> class.
        /// </summary>
        public ExtendedMasterDetailsView()
        {
            DefaultStyleKey = typeof(ExtendedMasterDetailsView);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call
        /// ApplyTemplate. In simplest terms, this means the method is called just before a UI element displays
        /// in your app. Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_inlineBackButton != null)
            {
                _inlineBackButton.Click -= OnInlineBackButtonClicked;
            }

            _inlineBackButton = (Button)GetTemplateChild(PartBackButton);
            if (_inlineBackButton != null)
            {
                _inlineBackButton.Click += OnInlineBackButtonClicked;
            }

            _detailsPresenter = (ContentPresenter)GetTemplateChild(PartDetailsPresenter);
            SetDetailsContent();

            SetMasterHeaderVisibility();
            OnDetailsCommandBarChanged();
            OnMasterCommandBarChanged();

            SizeChanged -= MasterDetailsView_SizeChanged;
            SizeChanged += MasterDetailsView_SizeChanged;

            DrawShadow();
            UpdateView(true);
        }

        /// <summary>
        /// Fired when the SelectedIndex changes.
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The event args</param>
        /// <remarks>
        /// Sets up animations for the DetailsPresenter for animating in/out.
        /// </remarks>
        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ExtendedMasterDetailsView)d;

            var newValue = (int)e.NewValue < 0 ? null : view.Items[(int)e.NewValue];
            var oldValue = e.OldValue == null ? null : view.Items.ElementAtOrDefault((int)e.OldValue);

            // check if selection actually changed
            if (view.SelectedItem != newValue)
            {
                if (newValue == null && view.SelectedItem != null && !view.Items.Contains(view.SelectedItem))
                {
                    newValue = view.SelectedItem;
                    view.SetValue(SelectedItemProperty, null);
                }

                // sync SelectedItem
                view.SetValue(SelectedItemProperty, newValue);
                view.UpdateSelection(oldValue, newValue);
            }
        }

        /// <summary>
        /// Fired when the SelectedItem changes.
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The event args</param>
        /// <remarks>
        /// Sets up animations for the DetailsPresenter for animating in/out.
        /// </remarks>
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ExtendedMasterDetailsView)d;
            var index = e.NewValue == null ? -1 : view.Items.IndexOf(e.NewValue);

            // check if selection actually changed
            if (view.SelectedIndex != index || (e.NewValue != null && index == -1))
            {
                // sync SelectedIndex
                view.SetValue(SelectedIndexProperty, index);
                view.UpdateSelection(e.OldValue, e.NewValue);
            }
        }

        /// <summary>
        /// Fired when the <see cref="MasterHeader"/> is changed.
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The event args</param>
        private static void OnMasterHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ExtendedMasterDetailsView)d;
            view.SetMasterHeaderVisibility();
        }

        /// <summary>
        /// Fired when the DetailsCommandBar changes.
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The event args</param>
        private static void OnDetailsCommandBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ExtendedMasterDetailsView)d;
            view.OnDetailsCommandBarChanged();
        }

        /// <summary>
        /// Fired when CompactModeThresholdWIdthChanged
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The event args</param>
        private static void OnCompactModeThresholdWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ExtendedMasterDetailsView)d).HandleStateChanges();
        }

        private static void OnIntermediateModeThresholdWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ExtendedMasterDetailsView)d).HandleStateChanges();
        }

        private static void OnBackButtonBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ExtendedMasterDetailsView)d;
            view.SetBackButtonVisibility();
        }

        /// <summary>
        /// Fired when the MasterCommandBar changes.
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The event args</param>
        private static void OnMasterCommandBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ExtendedMasterDetailsView)d;
            view.OnMasterCommandBarChanged();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignMode.DesignModeEnabled == false)
            {
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
                if (_frame != null)
                {
                    _frame.Navigating -= OnFrameNavigating;
                }

                _navigationView = this.FindAscendants().FirstOrDefault(p => p.GetType().FullName == "Microsoft.UI.Xaml.Controls.NavigationView");
                _frame = this.FindAscendant<Frame>();
                if (_frame != null)
                {
                    _frame.Navigating += OnFrameNavigating;
                }

                _selectionStateGroup = (VisualStateGroup)GetTemplateChild(SelectionStates);
                if (_selectionStateGroup != null)
                {
                    _selectionStateGroup.CurrentStateChanged += OnSelectionStateChanged;
                }

                UpdateView(true);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DesignMode.DesignModeEnabled == false)
            {
                SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
                if (_frame != null)
                {
                    _frame.Navigating -= OnFrameNavigating;
                }

                _selectionStateGroup = (VisualStateGroup)GetTemplateChild(SelectionStates);
                if (_selectionStateGroup != null)
                {
                    _selectionStateGroup.CurrentStateChanged -= OnSelectionStateChanged;
                    _selectionStateGroup = null;
                }
            }
        }

        private void MasterDetailsView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // if size is changing
            if ((e.PreviousSize.Width < CompactModeThresholdWidth && e.NewSize.Width >= CompactModeThresholdWidth) ||
                (e.PreviousSize.Width >= CompactModeThresholdWidth && e.NewSize.Width < CompactModeThresholdWidth) ||
                (e.PreviousSize.Width < IntermediateModeThresholdWidth && e.NewSize.Width >= IntermediateModeThresholdWidth) ||
                (e.PreviousSize.Width >= IntermediateModeThresholdWidth && e.NewSize.Width < IntermediateModeThresholdWidth))
            {
                HandleStateChanges();
            }
        }

        private void OnInlineBackButtonClicked(object sender, RoutedEventArgs e)
        {
            SelectedItem = null;
        }

        /// <summary>
        /// Raises SelectionChanged event and updates view.
        /// </summary>
        /// <param name="oldSelection">Old selection.</param>
        /// <param name="newSelection">New selection.</param>
        private void UpdateSelection(object oldSelection, object newSelection)
        {
            OnSelectionChanged(new SelectionChangedEventArgs(new List<object> { oldSelection }, new List<object> { newSelection }));

            UpdateView(true);

            // If there is no selection, do not remove the DetailsPresenter content but let it animate out.
            if (SelectedItem != null)
            {
                SetDetailsContent();
            }
        }

        private void HandleStateChanges()
        {
            UpdateView(true);
            SetListSelectionWithKeyboardFocusOnVisualStateChanged(ViewState);
        }

        /// <summary>
        /// Closes the details pane if we are in narrow state
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The event args</param>
        private void OnFrameNavigating(object sender, NavigatingCancelEventArgs args)
        {
            if ((args.NavigationMode == NavigationMode.Back) && (ViewState == MasterDetailsViewState.Details))
            {
                SelectedItem = null;
                args.Cancel = true;
            }
        }

        /// <summary>
        /// Closes the details pane if we are in narrow state
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The event args</param>
        private void OnBackRequested(object sender, BackRequestedEventArgs args)
        {
            if (ViewState == MasterDetailsViewState.Details)
            {
                // let the OnFrameNavigating method handle it if
                if (_frame == null || !_frame.CanGoBack)
                {
                    SelectedItem = null;
                }

                args.Handled = true;
            }
        }

        private void SetMasterHeaderVisibility()
        {
            if (GetTemplateChild(PartHeaderContentPresenter) is FrameworkElement headerPresenter)
            {
                headerPresenter.Visibility = MasterHeader != null
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void DrawShadow()
        {
            try
            {
                var shadow = GetTemplateChild(PartMainShadow) as ThemeShadow;
                var mainPanel = GetTemplateChild(PartMasterPanel) as FrameworkElement;
                var details = GetTemplateChild(PartDetailsPanel) as FrameworkElement;
                if (shadow == null || mainPanel == null || details == null) return;
                shadow.Receivers.Add(mainPanel);
                details.Translation += new Vector3(0, 0, 16);
                ViewStateChanged += (sender, state) =>
                {
                    if (state == MasterDetailsViewState.Master || state == MasterDetailsViewState.Details)
                    {
                        shadow.Receivers.Clear();
                    }
                    else if (shadow.Receivers.Count == 0)
                    {
                        shadow.Receivers.Add(mainPanel);
                    }
                };
            }
            catch (Exception)
            {
                // Failed to set config Shadow. Maybe old system?
            }
        }

        private void UpdateView(bool animate)
        {
            UpdateViewState();
            SetVisualState(animate);
        }

        /// <summary>
        /// Sets the back button visibility based on the current visual state and selected item
        /// </summary>
        private void SetBackButtonVisibility(MasterDetailsViewState? previousState = null)
        {
            const int backButtonVisible = 1;

            if (DesignMode.DesignModeEnabled)
            {
                return;
            }

            if (ViewState == MasterDetailsViewState.Details)
            {
                if ((BackButtonBehavior == BackButtonBehavior.Inline) && (_inlineBackButton != null))
                {
                    _inlineBackButton.Visibility = Visibility.Visible;
                }
                else if (BackButtonBehavior == BackButtonBehavior.Automatic)
                {
                    // Continue to support the system back button if it is being used
                    var navigationManager = SystemNavigationManager.GetForCurrentView();
                    if (navigationManager.AppViewBackButtonVisibility == AppViewBackButtonVisibility.Visible)
                    {
                        // Setting this indicates that the system back button is being used
                        _previousSystemBackButtonVisibility = navigationManager.AppViewBackButtonVisibility;
                    }
                    else if ((_inlineBackButton != null) && ((_navigationView == null) || (_frame == null)))
                    {
                        // We can only use the new NavigationView if we also have a Frame
                        // If there is no frame we have to use the inline button
                        _inlineBackButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SetNavigationViewBackButtonState(backButtonVisible, true);
                    }
                }
                else if (BackButtonBehavior != BackButtonBehavior.Manual)
                {
                    var navigationManager = SystemNavigationManager.GetForCurrentView();
                    _previousSystemBackButtonVisibility = navigationManager.AppViewBackButtonVisibility;

                    navigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                }
            }
            else if (previousState == MasterDetailsViewState.Details)
            {
                if ((BackButtonBehavior == BackButtonBehavior.Inline) && (_inlineBackButton != null))
                {
                    _inlineBackButton.Visibility = Visibility.Collapsed;
                }
                else if (BackButtonBehavior == BackButtonBehavior.Automatic)
                {
                    if (_previousSystemBackButtonVisibility.HasValue == false)
                    {
                        if ((_inlineBackButton != null) && ((_navigationView == null) || (_frame == null)))
                        {
                            _inlineBackButton.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            SetNavigationViewBackButtonState(_previousNavigationViewBackVisibilty, _previousNavigationViewBackEnabled);
                        }
                    }
                }

                if (_previousSystemBackButtonVisibility.HasValue)
                {
                    // Make sure we show the back button if the stack can navigate back
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = _previousSystemBackButtonVisibility.Value;
                    _previousSystemBackButtonVisibility = null;
                }
            }
        }

        private void UpdateViewState()
        {
            var previousState = ViewState;

            if (ActualWidth < CompactModeThresholdWidth)
            {
                ViewState = SelectedItem == null ? MasterDetailsViewState.Master : MasterDetailsViewState.Details;
            }
            else
            {
                ViewState = MasterDetailsViewState.Both;
            }

            if (previousState != ViewState)
            {
                ViewStateChanged?.Invoke(this, ViewState);
                SetBackButtonVisibility(previousState);
            }
        }

        private void SetVisualState(bool animate)
        {
            string state;
            string noSelectionState;
            string hasSelectionState;
            if (ActualWidth < CompactModeThresholdWidth)
            {
                state = NarrowState;
                noSelectionState = NoSelectionNarrowState;
                hasSelectionState = HasSelectionNarrowState;
            }
            else if (ActualWidth < IntermediateModeThresholdWidth)
            {
                state = IntermediateState;
                noSelectionState = NoSelectionWideState;
                hasSelectionState = HasSelectionWideState;
            }
            else
            {
                state = WideState;
                noSelectionState = NoSelectionWideState;
                hasSelectionState = HasSelectionWideState;
            }

            VisualStateManager.GoToState(this, state, animate);
            VisualStateManager.GoToState(this, SelectedItem == null ? noSelectionState : hasSelectionState, animate);
        }

        private void SetNavigationViewBackButtonState(int visible, bool enabled)
        {
            if (_navigationView == null)
            {
                return;
            }

            var navType = _navigationView.GetType();
            var visibleProperty = navType.GetProperty("IsBackButtonVisible");
            if (visibleProperty != null)
            {
                _previousNavigationViewBackVisibilty = (int)visibleProperty.GetValue(_navigationView);
                visibleProperty.SetValue(_navigationView, visible);
            }

            var enabledProperty = navType.GetProperty("IsBackEnabled");
            if (enabledProperty != null)
            {
                _previousNavigationViewBackEnabled = (bool)enabledProperty.GetValue(_navigationView);
                enabledProperty.SetValue(_navigationView, enabled);
            }
        }

        private void SetDetailsContent()
        {
            if (_detailsPresenter != null)
            {
                _detailsPresenter.Content = MapDetails == null
                    ? SelectedItem
                    : SelectedItem != null ? MapDetails(SelectedItem) : null;
            }
        }

        private void OnMasterCommandBarChanged()
        {
            OnCommandBarChanged("MasterCommandBarPanel", MasterCommandBar);
        }

        private void OnDetailsCommandBarChanged()
        {
            OnCommandBarChanged("DetailsCommandBarPanel", DetailsCommandBar);
        }

        private void OnCommandBarChanged(string panelName, CommandBar commandbar)
        {
            var panel = GetTemplateChild(panelName) as Panel;
            if (panel == null)
            {
                return;
            }

            panel.Children.Clear();
            if (commandbar != null)
            {
                panel.Children.Add(commandbar);
            }
        }

        /// <summary>
        /// Sets whether the selected item should change when focused with the keyboard based on the view state
        /// </summary>
        /// <param name="viewState">the view state</param>
        private void SetListSelectionWithKeyboardFocusOnVisualStateChanged(MasterDetailsViewState viewState)
        {
            if (viewState == MasterDetailsViewState.Both)
            {
                SetListSelectionWithKeyboardFocus(true);
            }
            else
            {
                SetListSelectionWithKeyboardFocus(false);
            }
        }

        /// <summary>
        /// Sets whether the selected item should change when focused with the keyboard
        /// </summary>
        private void SetListSelectionWithKeyboardFocus(bool singleSelectionFollowsFocus)
        {
            if (GetTemplateChild("MasterList") is Windows.UI.Xaml.Controls.ListViewBase masterList)
            {
                masterList.SingleSelectionFollowsFocus = singleSelectionFollowsFocus;
            }
        }

        /// <summary>
        /// Fires when the selection state of the control changes
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">the event args</param>
        /// <remarks>
        /// Sets focus to the item list when the viewState is not Details.
        /// Sets whether the selected item should change when focused with the keyboard.
        /// </remarks>
        private void OnSelectionStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            SetFocus(ViewState);
            SetListSelectionWithKeyboardFocusOnVisualStateChanged(ViewState);
        }

        /// <summary>
        /// Sets focus to the relevant control based on the viewState.
        /// </summary>
        /// <param name="viewState">the view state</param>
        private void SetFocus(MasterDetailsViewState viewState)
        {
            if (viewState == MasterDetailsViewState.Master)
            {
                FocusItemList();
            }
            else
            {
                FocusFirstFocusableElementInDetails();
            }
        }

        /// <summary>
        /// Sets focus to the first focusable element in the details template
        /// </summary>
        private void FocusFirstFocusableElementInDetails()
        {
            if (GetTemplateChild(PartDetailsPanel) is DependencyObject details)
            {
                var focusableElement = string.IsNullOrEmpty(DetailsElementToFocus)
                    ? FocusManager.FindFirstFocusableElement(details)
                    : details.FindDescendantByName(DetailsElementToFocus);
                (focusableElement as Control)?.Focus(FocusState.Programmatic);
            }
        }

        /// <summary>
        /// Sets focus to the item list
        /// </summary>
        private void FocusItemList()
        {
            if (GetTemplateChild("MasterList") is Control masterList)
            {
                masterList.Focus(FocusState.Programmatic);
            }
        }
    }
}