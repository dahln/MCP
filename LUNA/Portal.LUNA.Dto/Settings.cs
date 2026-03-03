namespace Portal.LUNA.Dto
{
    public class SystemSettings
    {
        public string? EmailApiKey { get; set; }
        public string? SystemEmailAddress { get; set; }
        public bool RegistrationEnabled { get; set; } = true;
        public string? EmailDomainRestriction { get; set; }
        public string? PortalRootUrl { get; set; }
    }
}
