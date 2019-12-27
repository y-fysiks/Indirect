﻿using InstaSharper.Classes.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Classes.Models.User;
using System.ComponentModel;
using System.Threading;
using Windows.System;
using InstaSharper.Classes;
using InstaSharper.Helpers;
using Microsoft.Toolkit.Collections;

namespace InstantMessaging.Wrapper
{
    /// Wrapper of <see cref="InstaDirectInboxThread"/> with Observable lists
    class InstaDirectInboxThreadWrapper : InstaDirectInboxThread, INotifyPropertyChanged, IIncrementalSource<InstaDirectInboxItemWrapper>
    {
        private readonly IInstaApi _instaApi;

        public event PropertyChangedEventHandler PropertyChanged;

        public ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItemWrapper> ObservableItems { get; set; }
        public new ObservableCollection<InstaUserShortWrapper> Users { get; } = new ObservableCollection<InstaUserShortWrapper>();

        /// <summary>
        /// Only use this constructor to make empty placeholder thread.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="api"></param>
        public InstaDirectInboxThreadWrapper(InstaUserShort user, IInstaApi api)
        {
            ObservableItems = new ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItemWrapper>(this);
            _instaApi = api;
            Users.Add(new InstaUserShortWrapper(user, api));
            Title = user.UserName;
        }

        public InstaDirectInboxThreadWrapper(InstaRankedRecipientThread rankedThread, IInstaApi api)
        {
            ObservableItems = new ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItemWrapper>(this);
            _instaApi = api;
            Canonical = rankedThread.Canonical;
            Named = rankedThread.Named;
            Pending = rankedThread.Pending;
            Title = rankedThread.ThreadTitle;
            ThreadId = rankedThread.ThreadId;
            ThreadType = InstaDirectThreadType.Private;
            ViewerId = rankedThread.ViewerId;
            foreach (var user in rankedThread.Users.Select(x => new InstaUserShortWrapper(x, api)))
            {
                Users.Add(user);
            }
        }

        public InstaDirectInboxThreadWrapper(InstaDirectInboxThread source, IInstaApi api)
        {
            ObservableItems = new ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItemWrapper>(this);
            _instaApi = api;
            Canonical = source.Canonical;
            HasNewer = source.HasNewer;
            HasOlder = source.HasOlder;
            IsSpam = source.IsSpam;
            Muted = source.Muted;
            Named = source.Named;
            Pending = source.Pending;
            ViewerId = source.ViewerId;
            LastActivity = source.LastActivity;
            ThreadId = source.ThreadId;
            OldestCursor = source.OldestCursor;
            IsGroup = source.IsGroup;
            IsPin = source.IsPin;
            ValuedRequest = source.ValuedRequest;
            PendingScore = source.PendingScore;
            VCMuted = source.VCMuted;
            ReshareReceiveCount = source.ReshareReceiveCount;
            ReshareSendCount = source.ReshareSendCount;
            ExpiringMediaReceiveCount = source.ExpiringMediaReceiveCount;
            ExpiringMediaSendCount = source.ExpiringMediaSendCount;
            NewestCursor = source.NewestCursor;
            ThreadType = source.ThreadType;
            Title = source.Title;
            MentionsMuted = source.MentionsMuted;

            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem;
            LeftUsers = source.LeftUsers;
            LastSeenAt = source.LastSeenAt;
            HasUnreadMessage = source.HasUnreadMessage;

            foreach (var instaUserShortFriendship in source.Users)
            {
                var user = new InstaUserShortFriendshipWrapper(instaUserShortFriendship, api);
                Users.Add(user);
            }

            UpdateItemList(source.Items);
        }

        public void Update(InstaDirectInboxThread source)
        {
            UpdateExcludeItemList(source);
            UpdateItemList(source.Items);
        }

        private void UpdateExcludeItemList(InstaDirectInboxThread source)
        {
            Canonical = source.Canonical;
            //HasNewer = source.HasNewer;
            //HasOlder = source.HasOlder;
            IsSpam = source.IsSpam;
            Muted = source.Muted;
            Named = source.Named;
            Pending = source.Pending;
            ViewerId = source.ViewerId;
            LastActivity = source.LastActivity;
            ThreadId = source.ThreadId;
            IsGroup = source.IsGroup;
            IsPin = source.IsPin;
            ValuedRequest = source.ValuedRequest;
            PendingScore = source.PendingScore;
            VCMuted = source.VCMuted;
            ReshareReceiveCount = source.ReshareReceiveCount;
            ReshareSendCount = source.ReshareSendCount;
            ExpiringMediaReceiveCount = source.ExpiringMediaReceiveCount;
            ExpiringMediaSendCount = source.ExpiringMediaSendCount;
            ThreadType = source.ThreadType;
            Title = source.Title;
            MentionsMuted = source.MentionsMuted;

            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem.TimeStamp > LastPermanentItem.TimeStamp ?
                source.LastPermanentItem : LastPermanentItem;
            LeftUsers = source.LeftUsers;
            LastSeenAt = source.LastSeenAt;
            HasUnreadMessage = source.HasUnreadMessage;

            if (string.IsNullOrEmpty(OldestCursor) || 
                string.Compare(OldestCursor, source.OldestCursor, StringComparison.Ordinal) > 0)
            {
                OldestCursor = source.OldestCursor;
                HasOlder = source.HasOlder;
            }

            if (string.IsNullOrEmpty(NewestCursor) || 
                string.Compare(NewestCursor, source.NewestCursor, StringComparison.Ordinal) < 0)
            {
                NewestCursor = source.NewestCursor;
                HasNewer = HasNewer;
            }

            UpdateUserList(source.Users);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private void UpdateItemList(ICollection<InstaDirectInboxItem> source)
        {
            if (source == null) return;
            var convertedSource = source.Select(x => new InstaDirectInboxItemWrapper(x, _instaApi));
            if (ObservableItems.Count == 0)
            {
                foreach (var item in convertedSource)
                    ObservableItems.Add(item);
            }
            else
            {
                foreach (var item in convertedSource)
                {
                    var existingItem = ObservableItems.SingleOrDefault(x => x.Equals(item));
                    var existed = existingItem != null;

                    if (existed)
                    {
                        if (item.Reactions != null)
                        {
                            if (existingItem.Reactions == null) existingItem.Reactions = item.Reactions;
                            else existingItem.Reactions.Update(item.Reactions, Users, ViewerId);
                        }
                        continue;
                    }
                    for (var i = ObservableItems.Count-1; i >= 0; i--)
                    {
                        if (item.TimeStamp > ObservableItems[i].TimeStamp)
                        {
                            ObservableItems.Insert(i+1, item);
                            break;
                        }

                        if (i == 0)
                        {
                            ObservableItems.Insert(0, item);
                        }
                    }
                }
            }
        }

        private void UpdateUserList(List<InstaUserShortFriendship> users)
        {
            var toBeAdded = users.Where(p2 => Users.All(p1 => !p1.Equals(p2)));
            var toBeDeleted = Users.Where(p1 => users.All(p2 => !p1.Equals(p2)));
            foreach (var user in toBeAdded.Select(x => new InstaUserShortFriendshipWrapper(x, _instaApi)))
            {
                Users.Add(user);
            }
            foreach (var user in toBeDeleted)
            {
                Users.Remove(user);
            }
        }

        public async Task LoadOlderItems()
        {
            var pagination = PaginationParameters.MaxPagesToLoad(1);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.MessagingProcessor.GetThreadAsync(ThreadId, pagination);
            if (result.Succeeded)
            {
                Update(result.Value);
            }
        }

        public async Task<IEnumerable<InstaDirectInboxItemWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            // Without ThreadId we cant fetch thread items.
            if (string.IsNullOrEmpty(ThreadId) || !(HasOlder ?? true)) return new List<InstaDirectInboxItemWrapper>();
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.MessagingProcessor.GetThreadAsync(ThreadId, pagination);
            if (!result.Succeeded || result.Value.Items == null) return new List<InstaDirectInboxItemWrapper>();
            UpdateExcludeItemList(result.Value);
            return result.Value.Items.Select(x => new InstaDirectInboxItemWrapper(x, _instaApi));
        }
    }
}
