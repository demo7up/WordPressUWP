﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WordPressUWP.Services;
using WordPressPCL.Models;
using WordPressUWP.Interfaces;
using System.Diagnostics;
using Microsoft.Toolkit.Uwp;
using Windows.ApplicationModel.DataTransfer;
using System.Net;
using Windows.UI.Xaml.Navigation;
using CommonServiceLocator;
using GalaSoft.MvvmLight.Messaging;
using Windows.ApplicationModel.Resources;

namespace WordPressUWP.ViewModels
{
    public class KodiViewModel : ViewModelBase
    {
        private IWordPressService _wordPressService;
        private int _postid;
        private bool _firstLaunch = true;
        private DataTransferManager _dataTransferManager;

        public NavigationServiceEx NavigationService
        {
            get
            {
                return ServiceLocator.Current.GetInstance<NavigationServiceEx>();
            }
        }

        private const string NarrowStateName = "NarrowState";
        private const string WideStateName = "WideState";

        private VisualState _currentState;

        private ICommand _itemClickCommand;

        public ICommand ItemClickCommand
        {
            get
            {
                if (_itemClickCommand == null)
                {
                    _itemClickCommand = new RelayCommand<ItemClickEventArgs>(OnItemClick);
                }

                return _itemClickCommand;
            }
        }

        private ICommand _stateChangedCommand;

        public ICommand StateChangedCommand
        {
            get
            {
                if (_stateChangedCommand == null)
                {
                    _stateChangedCommand = new RelayCommand<VisualStateChangedEventArgs>(OnStateChanged);
                }

                return _stateChangedCommand;
            }
        }

        public IncrementalLoadingCollection<PostsService, Post> Posts;

        private Post _selectedPost;
        public Post SelectedPost
        {
            get { return _selectedPost; }
            set { Set(ref _selectedPost, value); }
        }

        private ObservableCollection<CommentThreaded> _comments;
        public ObservableCollection<CommentThreaded> Comments
        {
            get { return _comments; }
            set { Set(ref _comments, value); }
        }

        private string _commentInput;
        public string CommentInput
        {
            get { return _commentInput; }
            set { Set(ref _commentInput, value); }
        }

        private Comment _commentReply;
        public Comment CommentReply
        {
            get { return _commentReply; }
            set { Set(ref _commentReply, value); }
        }

        private bool _isCommentsLoading;
        public bool IsCommentsLoading
        {
            get { return _isCommentsLoading; }
            set { Set(ref _isCommentsLoading, value); }
        }


        private bool _isCommenting;
        public bool IsCommenting
        {
            get { return _isCommenting; }
            set { Set(ref _isCommenting, value); }
        }

        public KodiViewModel(IWordPressService wordPressService)
        {
            _wordPressService = wordPressService;
        }

        internal async void Init(VisualState currentState, NavigationEventArgs e)
        {
            // Show post if there's an id being passed along
            if (e != null && e.Parameter is string id && _firstLaunch)
            {
                _firstLaunch = false;
                if (!string.IsNullOrEmpty(id))
                {
                    Int32.TryParse(id, out _postid);
                    await RefreshPosts();
                }
            }

            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            LoadDataAsync(currentState);
        }

        public async Task RefreshPosts()
        {
            Debug.WriteLine("RefreshPosts");
            var res = ResourceLoader.GetForCurrentView();
            if (Posts != null && !Posts.IsLoading && Posts.Count > 0 && !_wordPressService.IsLoadingPosts)
            {
                try
                {
                    Posts.Refresh();
                }
                catch (Exception ex)
                {
                    MessengerInstance.Send(new NotificationMessage(res.GetString("Notification_RefreshFailed")));
                }
            } else
            {
                Debug.WriteLine("Refresh denied");
            }
        }

        private async Task GetComments(int postid)
        {
            CommentReply = null;
            IsCommentsLoading = true;
            if (Comments != null)
            {
                Comments.Clear();
            }

            var comments = await _wordPressService.GetCommentsForPost(postid);
            if (comments != null)
            {
                Comments = new ObservableCollection<CommentThreaded>(comments);
            }
            IsCommentsLoading = false;
        }

        public async Task PostComment()
        {
            var res = ResourceLoader.GetForCurrentView();
            try
            {
                IsCommenting = true;

                if (await _wordPressService.IsUserAuthenticated())
                {
                    int replyto = 0;
                    if (CommentReply != null)
                        replyto = CommentReply.Id;
                    var comment = await _wordPressService.PostComment(SelectedPost.Id, CommentInput, replyto);
                    if (comment != null)
                    {
                        MessengerInstance.Send(new NotificationMessage(res.GetString("Notification_CommentPosted")));
                        CommentInput = String.Empty;
                        await GetComments(SelectedPost.Id);
                        CommentReplyUnset();
                    }
                    else
                    {
                        MessengerInstance.Send(new NotificationMessage(res.GetString("Notification_GenericError")));
                    }
                }
                else
                {
                    MessengerInstance.Send(new NotificationMessage(res.GetString("Notification_Unauthenticated")));
                }
            }
            catch (WordPressPCL.Models.WPException e)
            {
                if (e.Message == res.GetString("Comments_Disabled"))
                {
                    MessengerInstance.Send(new NotificationMessage(res.GetString("Notification_CommentFalse")));
                }
                else if (e.Message == res.GetString("Comments_Duplicate"))
                {
                    MessengerInstance.Send(new NotificationMessage(res.GetString("Notification_Duplicate")));
                }
                else
                {
                    MessengerInstance.Send(new NotificationMessage(res.GetString("Notification_GenericError")));
                }
            }

            finally
            {
                IsCommenting = false;
            }
        }

        public void LoadDataAsync(VisualState currentState)
        {
            _currentState = currentState;
            if(Posts == null)
            {
                Posts = new IncrementalLoadingCollection<PostsService, Post>();
                Posts.OnEndLoading = PostsOnEndLoading;
            }
        }

        public void PostsOnEndLoading()
        {
            if (_postid != 0)
            {
                // a post id has been set by a push notification, show it
                var selectedPost = Posts.Where(x => x.Id == _postid).FirstOrDefault();
                if (selectedPost != null)
                    ShowPost(selectedPost);
            }
        }

        private void OnStateChanged(VisualStateChangedEventArgs args)
        {
            _currentState = args.NewState;
        }

        private void OnItemClick(ItemClickEventArgs args)
        {
            if (args?.ClickedItem is Post item)
            {
                ShowPost(item);
            }
        }

        private async void ShowPost(Post post)
        {
            if (_currentState.Name == NarrowStateName)
            {
                NavigationService.Navigate(typeof(NewsDetailViewModel).FullName, post);
            }
            else
            {
                SelectedPost = post;
                await GetComments(post.Id);
            }
        }

        public async void RefreshComments()
        {
            await GetComments(SelectedPost.Id);
        }

        public async void OpenInBrowser()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(SelectedPost.Link));
        }

        public void SharePost()
        {
            DataTransferManager.ShowShareUI();
        }

        public void CommentReplyUnset()
        {
            CommentReply = null;
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            try
            {
                request.Data.SetWebLink(new Uri(SelectedPost.Link));
                request.Data.Properties.Title = WebUtility.HtmlDecode(SelectedPost.Title.Rendered);
            }
            catch
            {
                Debug.WriteLine("Share Failed");
                //request.FailWithDisplayText("Enter the web link you would like to share and try again.");
            }
        }
    }
}
