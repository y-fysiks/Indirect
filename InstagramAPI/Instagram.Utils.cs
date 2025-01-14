﻿using System;
using Windows.Networking.Connectivity;
using Windows.Web.Http.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private void ValidateUser()
        {
            if ((string.IsNullOrEmpty(Session.Username) || string.IsNullOrEmpty(Session.Password)) &&
                string.IsNullOrEmpty(Session.FacebookAccessToken))
                throw new ArgumentException("user name and password or access token must be specified");
        }

        private async void ValidateLoggedIn()
        {
            try
            {
                ValidateUser();
                if (!IsUserAuthenticated)
                    throw new ArgumentException("user must be authenticated");
            }
            catch (ArgumentException)
            {
                // Saved data may be corrupted. Force logout.
                IsUserAuthenticated = false;
                await SaveToAppSettings();  // TODO: change to explicit delete once SessionManager is stable
                throw;
            }
        }

        private void ValidateRequestMessage()
        {
            if (_apiRequestMessage == null || _apiRequestMessage.IsEmpty())
                throw new ArgumentException("API request message null or empty");
        }

        private static string GenerateRandomString(int length)
        {
            var rand = new Random();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var result = "";
            for (var i = 0; i < length; i++)
            {
                result += pool[rand.Next(0, pool.Length)];
            }

            return result;
        }

        private static string GetRetryContext()
        {
            return new JObject
            {
                {"num_step_auto_retry", 0},
                {"num_reupload", 0},
                {"num_step_manual_retry", 0}
            }.ToString(Formatting.None);
        }

        public static bool InternetAvailable()
        {
            var internetProfile = NetworkInformation.GetInternetConnectionProfile();
            return internetProfile != null;
        }
    }
}
