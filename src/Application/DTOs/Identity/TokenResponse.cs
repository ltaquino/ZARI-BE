using System.Text.Json.Serialization;

namespace ZARI.Application.DTOs.Identity;

    public sealed record TokenResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string JWToken { get; set; } = string.Empty;
        public DateTime IssuedOn { get; set; }
        public DateTime ExpiresOn { get; set; }
        public bool IsVerified { get; set; }

        [JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;
    }

