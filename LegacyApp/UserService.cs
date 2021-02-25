using System;

namespace LegacyApp
{
    public class UserService
    {
        private Func<DateTime> _dateTimeNow;
        private ClientRepository _clientRepository;
        private UserCreditServiceClient _userCreditServiceClient;
        private Action<User> _userDataAccessAddUser;

        public UserService(Func<DateTime> now = null, ClientRepository clientRepository = null, UserCreditServiceClient userCreditServiceClient = null, Action<User> userDataAccessAddUser = null)
        {
            _clientRepository = clientRepository ?? new ClientRepository();
            _dateTimeNow = now ?? (() => DateTime.Now);
            _userCreditServiceClient = userCreditServiceClient ?? new UserCreditServiceClient();
            _userDataAccessAddUser = userDataAccessAddUser ?? ((user) => UserDataAccess.AddUser(user));
        }
        
        public bool AddUser(string firname, string surname, string email, DateTime dateOfBirth, int clientId)
        {
            if (string.IsNullOrEmpty(firname) || string.IsNullOrEmpty(surname))
            {
                return false;
            }

            if (!email.Contains("@") && !email.Contains("."))
            {
                return false;
            }
            
            var now = _dateTimeNow();
            int age = now.Year - dateOfBirth.Year;
            if (now.Month < dateOfBirth.Month || (now.Month == dateOfBirth.Month && now.Day < dateOfBirth.Day)) age--;

            if (age < 21)
            {
                return false;
            }

            var client = _clientRepository.GetById(clientId);

            var user = new User
                               {
                                   Client = client,
                                   DateOfBirth = dateOfBirth,
                                   EmailAddress = email,
                                   Firstname = firname,
                                   Surname = surname
                               };

            if (client.Name == "VeryImportantClient")
            {
                // Skip credit check
                user.HasCreditLimit = false;
            }
            else if (client.Name == "ImportantClient")
            {
                // Do credit check and double credit limit
                user.HasCreditLimit = true;
                using (var userCreditService = _userCreditServiceClient)
                {
                    var creditLimit = userCreditService.GetCreditLimit(user.Firstname, user.Surname, user.DateOfBirth);
                    creditLimit = creditLimit*2;
                    user.CreditLimit = creditLimit;
                }
            }
            else
            {
                // Do credit check
                user.HasCreditLimit = true;
                using (var userCreditService = _userCreditServiceClient)
                {
                    var creditLimit = userCreditService.GetCreditLimit(user.Firstname, user.Surname, user.DateOfBirth);
                    user.CreditLimit = creditLimit;
                }
            }

            if (user.HasCreditLimit && user.CreditLimit < 500)
            {
                return false;
            }

            _userDataAccessAddUser(user);

            return true;
        }
    }
}
