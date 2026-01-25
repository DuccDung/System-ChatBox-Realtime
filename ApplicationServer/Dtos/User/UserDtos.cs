namespace ApplicationServer.Dtos.User
{
    public class userDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhotoPath { get; set; } = string.Empty;
    }
}
