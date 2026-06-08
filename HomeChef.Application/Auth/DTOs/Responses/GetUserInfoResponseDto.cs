using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeChef.Application.Auth.DTOs.Responses
{
    public class GetUserInfoResponseDto
    {
      
        public string FirstName { get; set; }

        public string LastName { get; set; }
        public DateOnly BirthDate { get; set; }
        public EnGender Gender { get; set; }
        public string Email { get; set; }
    }
}
