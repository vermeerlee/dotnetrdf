/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VDS.Common.Collections;

namespace VDS.RDF
{
    /// <summary>
    /// Mapper class which remaps Blank Node GUIDs into string IDs for output
    /// </summary>
    public class BlankNodeOutputMapper
    {
        public const String DefaultOutputPrefix = "b";

        private readonly String _outputPrefix = DefaultOutputPrefix;
        private IDictionary<Guid, String> _mappings = new MultiDictionary<Guid, String>();
        private long _nextid = 0;

        /// <summary>
        /// Creates a new Blank Node ID mapper
        /// </summary>
        /// <param name="validator">Function which determines whether IDs are valid or not</param>
        [Obsolete("The BlankNodeOutputMapper no longer needs to validate the IDs, please use an alternative oveload of the constructor", true)]
        public BlankNodeOutputMapper(Func<String, bool> validator)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Creates a new mapper using the default settings
        /// </summary>
        public BlankNodeOutputMapper()
            : this(DefaultOutputPrefix) { }

        /// <summary>
        /// Creates a new mapper using a custom blank node prefix
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <remarks>
        /// <para>
        /// It is up to the user to ensure that the the prefix given will result in valid blank node identifiers for any usage the generated string IDs are put to
        /// </para>
        /// </remarks>
        public BlankNodeOutputMapper(String prefix)
        {
            this._outputPrefix = prefix.ToSafeString();
        }

        /// <summary>
        /// Takes a GUID and generates a valid output ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public String GetOutputID(Guid id)
        {
            lock (this._mappings)
            {
                String outputID;
                if (!this._mappings.TryGetValue(id, out outputID))
                {
                    outputID = this.GetNextID();
                    this._mappings.Add(id, outputID);
                }
                return outputID;
            }
        }

        /// <summary>
        /// Takes a ID, validates it and returns either the ID or an appropriate remapped ID
        /// </summary>
        /// <param name="id">ID to map</param>
        /// <returns></returns>
        [Obsolete("Obsolete, use the overload which takes a Guid instead", true)]
        public String GetOutputID(String id)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Internal Helper function which generates the new IDs
        /// </summary>
        /// <returns></returns>
        private String GetNextID()
        {
            String nextID = this._outputPrefix + Interlocked.Increment(ref this._nextid);

            return nextID;
        }
    }
}