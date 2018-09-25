﻿using System;
using GalaSoft.MvvmLight.Ioc;
using WordPressUWP.Services;
using WordPressUWP.Views;
using WordPressUWP.Interfaces;
using Microsoft.Toolkit.Collections;
using WordPressPCL.Models;
using CommonServiceLocator;

namespace WordPressUWP.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register(() => new NavigationServiceEx());
            SimpleIoc.Default.Register<IWordPressService, WordPressService>();
            SimpleIoc.Default.Register<IPushNotificationService, PushNotificationService>();
            SimpleIoc.Default.Register<IIncrementalSource<Post>, PostsService>();
            SimpleIoc.Default.Register<ShellViewModel>();
            SimpleIoc.Default.Register<SettingsService>();
            Register<NewsViewModel, NewsPage>();
            Register<KodiViewModel, KodiPage>();
            Register<NewsDetailViewModel, NewsDetailPage>();
            Register<SettingsViewModel, SettingsPage>();
        }

        public IWordPressService WordPressService => ServiceLocator.Current.GetInstance<IWordPressService>();

        public IPushNotificationService PushNotificationService => ServiceLocator.Current.GetInstance<IPushNotificationService>();

        public SettingsViewModel SettingsViewModel => ServiceLocator.Current.GetInstance<SettingsViewModel>();

        public NewsDetailViewModel NewsDetailViewModel => ServiceLocator.Current.GetInstance<NewsDetailViewModel>();

        public NewsViewModel NewsViewModel => ServiceLocator.Current.GetInstance<NewsViewModel>();

        public KodiViewModel KodiViewModel => ServiceLocator.Current.GetInstance<KodiViewModel>();

        public ShellViewModel ShellViewModel => ServiceLocator.Current.GetInstance<ShellViewModel>();

        public NavigationServiceEx NavigationService => ServiceLocator.Current.GetInstance<NavigationServiceEx>();

        public SettingsService SettingsSerive => ServiceLocator.Current.GetInstance<SettingsService>();



        public void Register<VM, V>()
            where VM : class
        {
            SimpleIoc.Default.Register<VM>();

            NavigationService.Configure(typeof(VM).FullName, typeof(V));
        }
    }
}
