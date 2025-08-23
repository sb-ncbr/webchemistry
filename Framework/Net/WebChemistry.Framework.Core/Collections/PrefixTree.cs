namespace WebChemistry.Framework.Core.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PrefixTreeMap<T>
    {
        PrefixTreeMapNode<T> RootNode { get; set; }

        public PrefixTreeMap(IEnumerable<T> xs, Func<T,string> selector)
        {
            RootNode = new PrefixTreeMapNode<T> { Letter = PrefixTreeMapNode<T>.Root };
            foreach (var x in xs) Add(selector(x), x);
        }

        public PrefixTreeMap()
        {
            RootNode = new PrefixTreeMapNode<T> { Letter = PrefixTreeMapNode<T>.Root };
        }

        /// <summary>
        /// Adds a word to the tree.
        /// </summary>
        /// <param name="word"></param>
        public void Add(string word, T value)
        {
            word = word.ToLower() + PrefixTreeMapNode<T>.Eow;
            var currentNode = RootNode;
            foreach (var c in word)
            {
                currentNode = currentNode.AddChild(c, value);
            }
        }
        
        /// <summary>
        /// Matches a prefix.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="maxMatches"></param>
        /// <returns>Dictionary with OrdinalIgnoreCase comparer.</returns>
        public Dictionary<string, T> Match(string prefix, int? maxMatches = null)
        {
            prefix = prefix.ToLower();

            var ret = new Dictionary<string, T>(System.StringComparer.OrdinalIgnoreCase);

            _MatchRecursive(RootNode, ret, "", prefix, maxMatches);
            return ret;
        }

        static char EowLetter = PrefixTreeMapNode<T>.Eow;

        private static void _MatchRecursive(PrefixTreeMapNode<T> node, Dictionary<string, T> rtn, string letters, string prefix, int? maxMatches)
        {
            if (maxMatches != null && rtn.Count == maxMatches)
                return;

            if (node.Letter == EowLetter)
            {
                rtn[letters] = node.Value;
                return;
            }

            letters += node.Letter.ToString();

            if (prefix.Length > 0)
            {
                if (node.ContainsKey(prefix[0]))
                {
                    _MatchRecursive(node[prefix[0]], rtn, letters, prefix.Substring(1), maxMatches);
                }
            }
            else
            {
                foreach (char key in node.Keys)
                {
                    _MatchRecursive(node[key], rtn, letters, prefix, maxMatches);
                }
            }
        }
    }

    class PrefixTreeMapNode<T>
    {
        public const char Eow = '$';
        public const char Root = ' ';

        public char Letter { get; set; }
        public T Value { get; set; }

        string KeyString;
        public Dictionary<char, PrefixTreeMapNode<T>> Children { get; private set; }

        public PrefixTreeMapNode() { }

        private PrefixTreeMapNode(char letter)
        {
            this.Letter = letter;
        }

        private PrefixTreeMapNode(T value)
        {
            this.Letter = Eow;
            this.Value = value;
        }

        public PrefixTreeMapNode<T> this[char index]
        {
            get { return Children[index]; }
        }

        public string Keys
        {
            get { return KeyString; }
        }

        public bool ContainsKey(char key)
        {
            return Children.ContainsKey(key);
        }

        public PrefixTreeMapNode<T> AddChild(char letter, T value)
        {
            if (Children == null)
            {
                KeyString = "";
                Children = new Dictionary<char, PrefixTreeMapNode<T>>();
            }

            if (!Children.ContainsKey(letter))
            {
                var node = letter != Eow ? new PrefixTreeMapNode<T>(letter) : new PrefixTreeMapNode<T>(value);
                KeyString += letter.ToString();
                Children.Add(letter, node);
                return node;
            }

            return Children[letter];
        }

        public override string ToString()
        {
            return this.Letter.ToString();
        }
    }
}
