using System.Collections.Generic;

namespace FTP_Server
{
    public static class AccountManager
    {
        private static Dictionary<string, string> accounts;

        static AccountManager()
        {
            accounts = new Dictionary<string, string>();
        }

        public static bool Add(string login, string password)
        {
            if (accounts.ContainsKey(login))
            {
                return false;
            }
            else
            {
                accounts.Add(login, password);
                return true;
            }
        }

        public static bool IsSutable(string login, string password)
        {
            if (accounts.ContainsKey(login))
            {
                return accounts[login] == password;
            }
            return false;
        }

        public static bool IsSutable(string login)
        {
            return accounts.ContainsKey(login);
        }
    }
}
