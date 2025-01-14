﻿using System;
using System.Collections.Generic;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Windows.Web.Http;
using InstagramAPI.Classes.Android;

namespace InstagramAPI.Classes.Core
{
    public class UserSessionData
    {
        private string _csrfToken;

        [JsonProperty]
        public string Username { get; internal set; }

        [JsonProperty]
        public string Password { get; internal set; }

        [JsonProperty]
        public BaseUser LoggedInUser { get; internal set; }

        [JsonProperty]
        public string RankToken { get; internal set; }

        [JsonIgnore]
        public string CsrfToken
        {
            get
            {
                if (string.IsNullOrEmpty(_csrfToken))
                    _csrfToken = Instagram.GetCsrfToken();
                return _csrfToken;
            }
        }

        /// <summary>
        ///     Only for facebook login
        /// </summary>
        [JsonProperty]
        public string FacebookUserId { get; internal set; } = string.Empty;

        [JsonProperty]
        public string FacebookAccessToken { get; internal set; } = string.Empty;

        [JsonProperty(ItemConverterType = typeof(HttpCookieConverter))]
        internal List<HttpCookie> Cookies { get; set; }

        [JsonProperty]
        internal FbnsConnectionData PushData { get; set; }

        [JsonProperty]
        internal AndroidDevice Device { get; set; }

        public void SaveToAppSettings()
        {
            if (LoggedInUser == null) return;
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = new Windows.Storage.ApplicationDataCompositeValue
            {
                ["Username"] = Username,
                ["Password"] = Password,
                ["RankToken"] = RankToken,
                ["FacebookUserId"] = FacebookUserId,
                ["FacebookAccessToken"] = FacebookAccessToken,
                ["LoggedInUser.IsVerified"] = LoggedInUser.IsVerified,
                ["LoggedInUser.IsPrivate"] = LoggedInUser.IsPrivate,
                ["LoggedInUser.Pk"] = LoggedInUser.Pk,
                ["LoggedInUser.ProfilePictureUrl"] = LoggedInUser.ProfilePictureUrl.ToString(),
                ["LoggedInUser.ProfilePictureId"] = LoggedInUser.ProfilePictureId,
                ["LoggedInUser.Username"] = LoggedInUser.Username,
                ["LoggedInUser.FullName"] = LoggedInUser.FullName
            };
            localSettings.Values["_userSessionData"] = composite;
        }

        public void LoadFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["_userSessionData"];
            if (composite == null) return;
            Username = (string)composite["Username"];
            Password = (string)composite["Password"];
            RankToken = (string)composite["RankToken"];
            FacebookUserId = (string)composite["FacebookUserId"];
            FacebookAccessToken = (string)composite["FacebookAccessToken"];
            LoggedInUser = new BaseUser
            {
                IsVerified = (bool)composite["LoggedInUser.IsVerified"],
                IsPrivate = (bool)composite["LoggedInUser.IsPrivate"],
                Pk = (long)composite["LoggedInUser.Pk"],
                ProfilePictureUrl = new Uri((string)composite["LoggedInUser.ProfilePictureUrl"]),
                ProfilePictureId = (string)composite["LoggedInUser.ProfilePictureId"],
                Username = (string)composite["LoggedInUser.Username"],
                FullName = (string)composite["LoggedInUser.FullName"]
            };

            //var activeSessionName = SessionManager.SessionUsername;
            //if (string.IsNullOrEmpty(activeSessionName))
            //{
            //    return;
            //}

            //var test = await SessionManager.TryLoadSessionAsync(activeSessionName);
        }

        public static UserSessionData CreateFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["_userSessionData"];
            if (composite == null) return null;
            var session = new UserSessionData();
            session.LoadFromAppSettings();
            return session;
        }

        public void RemoveFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove("_userSessionData");
        }
    }
}