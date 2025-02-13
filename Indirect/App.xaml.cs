﻿using System;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Indirect.Pages;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Utils;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.UI;

namespace Indirect
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        internal MainViewModel ViewModel { get; } = new MainViewModel();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if !DEBUG
            AppCenter.Start(Secrets.APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
#endif
            this.InitializeComponent();
            SetTheme();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
            this.EnteredBackground += OnEnteredBackground;
            ImageCache.Instance.CacheDuration = TimeSpan.FromDays(7);
        }

        private void SetTheme()
        {
            var requestedTheme = _localSettings.Values["Theme"] as string;
            if (requestedTheme == null) return;
            switch (requestedTheme)
            {
                case "Dark":
                    RequestedTheme = ApplicationTheme.Dark;
                    break;

                case "Light":
                    RequestedTheme = ApplicationTheme.Light;
                    break;
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            OnLaunchedOrActivated(args);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            OnLaunchedOrActivated(e);
        }

        private async void OnLaunchedOrActivated(IActivatedEventArgs e)
        {
            if (e is ContactPanelActivatedEventArgs cpEventArgs)
            {
                // Contact Panel flow
                var rootFrame = new Frame();
                Window.Current.Content = rootFrame;
                rootFrame.Navigate(typeof(ContactPanelPage), cpEventArgs);
            }
            else
            {
                // Normal launch or activated flow
                ViewModel.StartedFromMainView = true;
                Frame rootFrame = Window.Current.Content as Frame;

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    await ViewModel.TryAcquireSyncLock();
                    ConfigureMainView();

                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();

                    rootFrame.NavigationFailed += OnNavigationFailed;

                    if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        // Handle different ExecutionStates
                    }

                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }

                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(Instagram.IsUserAuthenticatedPersistent ? typeof(MainPage) : typeof(LoginPage));
                }

                if (e is ToastNotificationActivatedEventArgs toastActivationArgs)
                {
                    var launchArgs = HttpUtility.ParseQueryString(toastActivationArgs.Argument);
                    var threadId = launchArgs["threadId"];
                    ViewModel.OpenThreadWhenReady(threadId);
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            if (!ViewModel.IsUserAuthenticated) return;
            var deferral = e.SuspendingOperation.GetDeferral();
            try
            {
                ViewModel.ReelsFeed.StopReelsFeedUpdateLoop();
                ViewModel.SyncClient.Shutdown();    // Shutdown cleanly is not important here.
                await ViewModel.PushClient.TransferPushSocket();
                ViewModel.ReleaseSyncLock();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception, false);
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnResuming(object sender, object e)
        {
            if (!ViewModel.IsUserAuthenticated) return;
            if (ViewModel.StartedFromMainView)
            {
                await ViewModel.TryAcquireSyncLock();
                ViewModel.PushClient.Start();
                await ViewModel.SyncClient.Start(ViewModel.Inbox.SeqId, ViewModel.Inbox.SnapshotAt, true);
                await ViewModel.UpdateInboxAndSelectedThread();
                ViewModel.ReelsFeed.StartReelsFeedUpdateLoop();
            }
            else
            {
                await ViewModel.SyncClient.Start(ViewModel.Inbox.SeqId, ViewModel.Inbox.SnapshotAt, true);
            }
        }

        private async void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                await ViewModel.SaveToAppSettings();
            }
            finally
            {
                deferral.Complete();
            }
        }

        private static void ConfigureMainView()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(380, 300));
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
        }

        public static async Task CreateAndShowNewView(Type targetPage, object parameter = null, CoreApplicationView view = null)
        {
            var newView = view ?? CoreApplication.CreateNewView();
            await newView.Dispatcher.QuickRunAsync(async () =>
            {
                var newAppView = ApplicationView.GetForCurrentView();
                newAppView.SetPreferredMinSize(new Size(380, 300));
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

                var frame = new Frame();
                frame.Navigate(targetPage, parameter);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                var newViewId = ApplicationView.GetForCurrentView().Id;
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
                newAppView.TryResizeView(new Size(380, 640));
            });
        }
    }
}
