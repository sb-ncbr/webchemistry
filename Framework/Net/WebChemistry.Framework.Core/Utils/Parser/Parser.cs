namespace WebChemistry.Framework.Core.Utils
{
    /// <summary>
    /// Parser wrapper
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Parse an input string and return the best match.
        /// </summary>
        /// <param name="rootRule"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Node Parse(Rule rootRule, string input)
        {
            return rootRule.Parse(input)[0];
        }
    }
}
