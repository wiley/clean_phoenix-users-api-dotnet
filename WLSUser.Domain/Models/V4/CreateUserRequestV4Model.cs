﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WLSUser.Domain.Models.V4
{
    public class CreateUserRequestV4Model
    {
        [StringLength(255), Required]
        public string Username { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(50), Required]
        public string Password { get; set; }

        [StringLength(255)]
        public string AlmId { get; set; }

        [StringLength(255)]
        public string Email { get; set; }

        public DateTime? DataConsentDate { get; set; }

        public DateTime? PrivacyAcceptDate { get; set; }

        [StringLength(255)]
        public string RecoveryEmail { get; set; } = "";

        [StringLength(50)]
        public string Language { get; set; }

        [StringLength(50)]
        public string PhoneNumber { get; set; }

        [StringLength(120)]
        public string AddressLine1 { get; set; }

        [StringLength(100)]
        public string AddressLine2 { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string Region { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(50)]
        public string PostalCode { get; set; }

        public ImageAPISaveImageRequest AvatarImage { get; set; }

        public List<CreateUserMappingRequest> UserMappings { get; set; }
    }
}