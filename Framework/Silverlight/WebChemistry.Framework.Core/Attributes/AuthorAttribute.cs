namespace WebChemistry.Framework.Core
{
    using System;

    public class AuthorAttribute : Attribute
    {
        public AuthorAttribute(string name, string contact, string comment)
        {

        }

        public AuthorAttribute(string name, string contact)
            : this(name, contact, "")
        {

        }
    }
}
