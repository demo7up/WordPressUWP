﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;
using WordPressUWP.Helpers;
using WordPressUWP.Interfaces;
using WordPressUWP.Models;

namespace WordPressUWP.Services
{
    public class WordPressService : ViewModelBase, IWordPressService
    {
        private readonly SettingsService _settingsService;
        private readonly ApplicationDataContainer _localSettings;
        private readonly WordPressClient _client;

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get { return _isAuthenticated; }
            set { Set(ref _isAuthenticated, value); }
        }

        private User _currentUser;
        public User CurrentUser
        {
            get { return _currentUser; }
            set { Set(ref _currentUser, value); }
        }

        private bool _isLoadingPosts;
        public bool IsLoadingPosts
        {
            get { return _isLoadingPosts; }
            set { Set(ref _isLoadingPosts, value); }
        }

        public WordPressService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _localSettings = ApplicationData.Current.LocalSettings;
            _client = new WordPressClient(Config.WordPressUri);
            Init();
        }

        public async void Init()
        {
            IsAuthenticated = false;
            var username = _settingsService.GetSetting<string>("Username", () => null, SettingLocality.Roamed);
            if (username != null)
            {
                // get password
                var jwt = SettingsStorageExtensions.GetCredentialFromLocker(username);
                if (jwt != null && !string.IsNullOrEmpty(jwt.Password))
                {
                    // set jwt
                    _client.SetJWToken(jwt.Password);
                    IsAuthenticated = await _client.IsValidJWToken();
                    if (IsAuthenticated)
                    {
                        CurrentUser = await _client.Users.GetCurrentUser();
                    }
                }
            }
        }

        public async Task<bool> AuthenticateUser(string username, string password)
        {
            _client.AuthMethod = AuthMethod.JWT;
            try
            {
                await _client.RequestJWToken(username, password);
            }
            catch
            {
                // Authentication failed
            }
            var isAuthenticated = await IsUserAuthenticated();

            if (isAuthenticated)
            {
                // Store username & JWT token for logging in on next app launch
                _settingsService.SetSetting("Username", username, SettingLocality.Roamed);
                SettingsStorageExtensions.SaveCredentialsToLocker(username, _client.GetToken());
                CurrentUser = await _client.Users.GetCurrentUser();
            }

            return isAuthenticated;
        }

        public async Task<List<CommentThreaded>> GetCommentsForPost(int postid)
        {
            var comments = await _client.Comments.GetAllCommentsForPost(postid);
            var isDesc = _settingsService.GetSetting("CommentsOrderDesc", () => true, SettingLocality.Roamed);
            return ThreadedCommentsHelper.GetThreadedComments(comments, 2, isDesc);
        }

        public async Task<IEnumerable<Post>> GetLatestPosts(int page = 0, int perPage = 20)
        {
            IsLoadingPosts = true;
            page++;
            IEnumerable<Post> posts = new List<Post>();
            try
            {
                posts = await _client.Posts.Query(new PostsQueryBuilder()
                {
                    Page = page,
                    PerPage = perPage,
                    Embed = true
                });
            }
            catch
            {
                var res = ResourceLoader.GetForCurrentView();
                //Disabled this Exception Messasge because of the JSON Error: "The page number requested is larger than the number of pages available." if the criteria doesnt match
                //var msg = res.GetString("Notification_DownloadPostsFailed");
                //Messenger.Default.Send(new NotificationMessage(msg));
            }
            IsLoadingPosts = false;
            return posts;
        }

        public User GetUser()
        {
            return CurrentUser;
        }

        public async Task<bool> IsUserAuthenticated()
        {
            IsAuthenticated = await _client.IsValidJWToken();
            return IsAuthenticated;
        }

        public async Task<Comment> PostComment(int postId, string text, int replyto = 0)
        {
            var comment = new Comment(postId, text);
            if (replyto != 0)
            {
                comment.ParentId = replyto;
            }
            return await _client.Comments.Create(comment);
        }

        public async Task<bool> Logout()
        {
            _client.Logout();
            IsAuthenticated = await _client.IsValidJWToken();
            SettingsStorageExtensions.RemoveCredentialsFromLocker();
            return IsAuthenticated;
        }
    }
}
