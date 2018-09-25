﻿namespace WordPressUWP
{
    public static class Config
    {
        public const string BaseUri = "https://firemax.online/";
        public static string WordPressUri = $"{BaseUri}wp-json/";

        // Push Notification Settings
        public const bool NotificationsEnabled = false;
        public const string HubName = "NotificationHubName";
        public const string AccessSiganture = "Endpoint=";

        // Comments
        public static int CommentDepth = 3;

        // Authentication
        public static bool EnableLogin = true;

        // Style
        public static int DefaultFontSize = 16;
    }
}
