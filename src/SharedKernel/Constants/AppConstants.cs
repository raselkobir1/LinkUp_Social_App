namespace LinkUp.SharedKernel.Constants;

public static class AppConstants
{
    public static class Pagination
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int DefaultPage = 1;
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public static class Media
    {
        public const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB
        public const long MaxVideoSizeBytes = 100 * 1024 * 1024; // 100 MB
        public const string ProfilePictureFolder = "profile-pictures";
        public const string CoverPhotoFolder = "cover-photos";
        public const string PostMediaFolder = "posts";
        public const string ChatMediaFolder = "chat";
        public const string GroupPhotoFolder = "groups";
    }

    public static class Cache
    {
        public const int OnlineStatusTtlSeconds = 300;
        public const string OnlineUsersPrefix = "online:";
        public const string LastSeenPrefix = "lastseen:";
    }

    public static class SignalR
    {
        public const string ChatHub = "/hubs/chat";
        public const string NotificationHub = "/hubs/notification";
        public const string VideoCallHub = "/hubs/videocall";
    }

    public static class Policy
    {
        public const string AdminOnly = "AdminOnly";
        public const string AuthenticatedUser = "AuthenticatedUser";
    }
}
