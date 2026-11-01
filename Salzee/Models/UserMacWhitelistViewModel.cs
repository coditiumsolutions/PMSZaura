namespace Salzee.Models
{
    public class UserMacWhitelistViewModel
    {
        public List<User> Users { get; set; } = new();
        public string? SelectedUserId { get; set; }
        public User? SelectedUser { get; set; }
        public List<UserMacWhitelist> WhitelistedMacs { get; set; } = new();
        public List<BlockedMacLoginAttempt> BlockedAttempts { get; set; } = new();
    }
}
