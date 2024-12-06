using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WLSUser.Domain.Models.V4
{
    public class UserResponseModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; } = "";
        [JsonConverter(typeof(StringEnumConverter))]
        public UserStatus Status { get; set; } = UserStatus.Active;
        public string AlmId { get; set; }
        public DateTime? DataConsentDate { get; set; }
        public DateTime? PrivacyAcceptDate { get; set; }
        public string RecoveryEmail { get; set; } = "";
        public string Language { get; set; }
        public string PhoneNumber { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string AvatarUrl { get; set; }

        public List<UserMappingResponse> UserMappings { get; set; }
    }
}
