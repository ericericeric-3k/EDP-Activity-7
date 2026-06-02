// ============================================================
//  SessionManager.cs
//  Holds the currently logged-in user's data across all forms
// ============================================================
namespace ECommSystem
{
    public static class SessionManager
    {
        public static int    UserId   { get; set; }
        public static string Username { get; set; } = "";
        public static string Email    { get; set; } = "";
        public static string Role     { get; set; } = "";
        public static bool   IsActive { get; set; }

        public static void Clear()
        {
            UserId   = 0;
            Username = "";
            Email    = "";
            Role     = "";
            IsActive = false;
        }

        public static bool IsAdmin => Role == "admin";
    }
}
