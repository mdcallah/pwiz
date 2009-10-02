using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace pwiz.Topograph.Query
{
    public class Identifier
    {
        public Identifier(params String[] parts)
            : this(new IEnumerable<string>[] { parts })
        {
        }
        public Identifier(params IEnumerable<String>[] parts)
        {
            List<String> list = new List<string>();
            foreach (var sequence in parts)
            {
                list.AddRange(sequence);
            }
            Parts = new ReadOnlyCollection<string>(list);
        }
        public Identifier(Identifier parent, String name)
            : this(GetParts(parent), new[] { name })
        {
        }
        public Identifier(Identifier parent, Identifier descendent)
            : this(GetParts(parent), GetParts(descendent))
        {
        }
        public Identifier(String ancestor, Identifier descendents)
            : this(new[] { ancestor }, GetParts(descendents))
        {
        }
        public IList<String> Parts { get; private set; }

        public Identifier Parent
        {
            get
            {
                return RemoveSuffix(1);
            }
        }

        public Identifier RemovePrefix(int levels)
        {
            if (Parts.Count < levels)
            {
                return null;
            }
            String[] suffixParts = new string[Parts.Count - levels];
            for (int i = 0; i < suffixParts.Length; i++)
            {
                suffixParts[i] = Parts[levels + i];
            }
            return new Identifier(suffixParts);
        }

        public Identifier RemoveSuffix(int levels)
        {
            if (Parts.Count < levels)
            {
                return null;
            }
            String[] prefixParts = new string[Parts.Count - levels];
            for (int i = 0; i < prefixParts.Length; i++)
            {
                prefixParts[i] = Parts[i];
            }
            return new Identifier(prefixParts);
        }

        public bool StartsWith(Identifier identifier)
        {
            if (identifier == null)
            {
                return true;
            }
            if (identifier.Parts.Count > Parts.Count)
            {
                return false;
            }
            for (int i = 0; i < identifier.Parts.Count; i++)
            {
                if (Parts[i] != identifier.Parts[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            Identifier that = o as Identifier;
            if (that == null)
            {
                return false;
            }
            if (Parts.Count != that.Parts.Count)
            {
                return false;
            }
            for (int i = 0; i < Parts.Count; i++)
            {
                if (Parts[i] != that.Parts[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = 0;
            foreach (String part in Parts)
            {
                result = result * 31 + part.GetHashCode();
            }
            return result;
        }

        public override String ToString()
        {
            return String.Join(".", Parts.ToArray());
        }

        public static Identifier Parse(String str)
        {
            return new Identifier(str.Split('.'));
        }

        private static IList<String> GetParts(Identifier identifier)
        {
            if (identifier == null)
            {
                return new string[0];
            }
            return identifier.Parts;
        }
    }
}
