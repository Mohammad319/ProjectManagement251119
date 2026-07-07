namespace ProjectManagement.Shared.DTO.App
{
    public class AddressDTO
    {
        /// <summary>Free-text label such as "Huvudkontor" — shown as "Beskrivning" in the simplified address UI.</summary>
        public string Description { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;
        public string ZIPCode { get; set; } = string.Empty;
        public string Nr { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
