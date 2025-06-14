using GolfBAIST.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace GolfBAIST.TechnicalServices
{
    public class RegisterService
    {
        private readonly MemberService _membersService;
        private readonly MembershipApplicationService _membershipApplicationService;
        private readonly UserService _userService;
        private readonly ILogger<RegisterService> _logger;

        public RegisterService(MemberService membersService, UserService userService, MembershipApplicationService membershipApplicationService, ILogger<RegisterService> logger)
        {
            _membersService = membersService;
            _userService = userService;
            _membershipApplicationService = membershipApplicationService;
            _logger = logger;
        }

        public bool AddNewMember(Register register)
        {
            try
            {
                var application = new MembershipApplication
                {
                    FirstName = register.FirstName,
                    LastName = register.LastName,
                    Address = register.Address,
                    PostalCode = register.PostalCode,
                    Phone = register.Phone,
                    AlternatePhone = register.AlternatePhone,
                    Email = register.Email,
                    DateOfBirth = register.DateOfBirth,
                    Occupation = register.Occupation,
                    CompanyName = register.CompanyName,
                    ConsentToEmail = register.ConsentToEmail,
                    AgreementAcknowledged = register.AgreementAcknowledged,
                    MembershipTypeID = register.MembershipTypeID,
                    Sponsor1 = register.Sponsor1,
                    Sponsor2 = register.Sponsor2,
                    Status = "Pending",
                    SubmittedDate = DateTime.Now
                };

              
                _membershipApplicationService.SubmitApplication(application);

                _userService.CreateUser(register.Username, register.Password, register.Email, "Applicant", register.FirstName, register.LastName);


                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register new applicant");
                return false;
            }
        }

    }
}
