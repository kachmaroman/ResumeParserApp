﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ResumeParserApp.Model
{
    public class Resume
    {
        public Resume()
        {
            Languages = new HashSet<string>();
            SocialProfiles = new HashSet<string>();
            WorkExperience = new List<string>();
            Education = new List<string>();
            PictureData = new byte[0];
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Nationality { get; set; }
        public byte[] PictureData { get; set; }
        public HashSet<string> Languages { get; set; }
        public HashSet<string> SocialProfiles { get; set; }
        public List<string> WorkExperience { get; set; }
        public List<string> Education { get; set; }
    }
}
