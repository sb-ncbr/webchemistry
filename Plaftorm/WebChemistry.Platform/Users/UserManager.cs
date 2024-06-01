namespace WebChemistry.Platform.Users
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// User manager.
    /// </summary>
    public class UserManager : ManagerBase<UserManager, UserInfo, UserInfo.Index, object>
    {
        /// <summary>
        /// Get user id from name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetIdFromName(string name)
        {
            return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '@' || c == '.').ToArray());
        }

        /// <summary>
        /// Check if the user exists.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Exists(string name)
        {
            return UserInfo.Exists(Id.GetChildId(GetIdFromName(name)));
        }

        /// <summary>
        /// Returns repository info from the name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public UserInfo GetUserByName(string name)
        {
            return UserInfo.Load(Id.GetChildId(GetIdFromName(name)));
        }

        /// <summary>
        /// Get a user by his token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public UserInfo GetUserByToken(string token)
        {
            var user = ReadIndex().FirstOrDefault(e => e.Entry.Token.Equals(token, StringComparison.Ordinal));
            if (user != null)
            {
                return UserInfo.Load(user.Id);
            }

            throw new ArgumentException("There is no user with this token.");
        }

        protected override UserInfo LoadElement(EntityId id)
        {
            return UserInfo.Load(id);
        }

        /// <summary>
        /// Create the user if he does not exists.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public UserInfo GetOrCreateUserByName(string name)
        {
            var id = Id.GetChildId(GetIdFromName(name));
            if (UserInfo.Exists(id)) return UserInfo.Load(id);
            return CreateUser(name);
        }
                
        /// <summary>
        /// Create a new user.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public UserInfo CreateUser(string name)
        {
            if (Exists(name)) throw new InvalidOperationException(string.Format("The user called '{0}' already exists.", name));

            var user = UserInfo.Create(this, name);
            AddToIndex(user);
            return user;
        }
    }
}
