using System.Text.Json.Serialization;

namespace ApplicationServer.Dtos.Profile
{
    // Response DTO cho profile
    public class ProfileResponseDto
    {
        [JsonPropertyName("accountId")]
        public int AccountId { get; set; }

        [JsonPropertyName("accountName")]
        public string? AccountName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("photoPath")]
        public string? PhotoPath { get; set; }

        [JsonPropertyName("photoBackground")]
        public string? PhotoBackground { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public DateOnly? DateOfBirth { get; set; }

        [JsonPropertyName("gender")]
        public byte? Gender { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }
    }

    // Request DTO cho cập nhật profile
    public class UpdateProfileRequest
    {
        [JsonPropertyName("accountName")]
        public string? AccountName { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public DateOnly? DateOfBirth { get; set; }

        [JsonPropertyName("gender")]
        public byte? Gender { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("photoPath")]
        public string? PhotoPath { get; set; }

        [JsonPropertyName("photoBackground")]
        public string? PhotoBackground { get; set; }
    }
}
