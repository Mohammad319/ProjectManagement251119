using ProjectManagement.Shared.Constant;
using System;
using System.ComponentModel.DataAnnotations;


namespace ProjectManagement.Shared.DTO.Identity
{
    public class RegisterDto
    {
        public class Response
        {
            public bool IsSuccessfulRegistration { get; set; }
            public string Errors { get; set; } = string.Empty;
        }
    }
}
