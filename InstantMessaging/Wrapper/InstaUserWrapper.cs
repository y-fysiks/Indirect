﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstaSharper.API;
using InstaSharper.Classes.Models.User;

namespace InstantMessaging.Wrapper
{
    class InstaUserWrapper : InstaUserShortWrapper
    {
        public bool HasAnonymousProfilePicture { get; set; }
        public int FollowersCount { get; set; }
        public string FollowersCountByLine { get; set; }
        public string SocialContext { get; set; }
        public string SearchSocialContext { get; set; }
        public int MutualFollowers { get; set; }
        public int UnseenCount { get; set; }
        public InstaFriendshipShortStatus FriendshipStatus { get; set; }

        private readonly IInstaApi _instaApi;

        public InstaUserWrapper(InstaUserShort source, IInstaApi api) : base(source, api)
        {
            _instaApi = api;

            if (source is InstaUser user)
            {
                HasAnonymousProfilePicture = user.HasAnonymousProfilePicture;
                FollowersCount = user.FollowersCount;
                FollowersCountByLine = user.FollowersCountByLine;
                SocialContext = user.SocialContext;
                SearchSocialContext = user.SearchSocialContext;
                MutualFollowers = user.MutualFollowers;
                UnseenCount = user.UnseenCount;
                FriendshipStatus = user.FriendshipStatus;
            }
        }

    }
}
