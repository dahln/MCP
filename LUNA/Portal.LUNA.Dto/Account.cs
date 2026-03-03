namespace Portal.LUNA.Dto
{
    public class AccountEmail
    {
        public string Email { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public int Status { get; set; }
    }

    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsAdministrator { get; set; }
        public bool IsSelf { get; set; }
    }

    public class AuthenticationResponse
    {
        public bool Succeeded { get; set; }
        public bool Prompt2FA { get; set; }
        public List<string> ErrorList { get; set; } = new();
    }

    public class UserInfo
    {
        public string Email { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; }
        public Dictionary<string, string> Claims { get; set; } = new();
    }
}
