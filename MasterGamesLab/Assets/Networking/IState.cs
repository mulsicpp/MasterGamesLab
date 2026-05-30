using Networking;
using NUnit.Framework;
using System.Collections.Generic;

namespace Networking
{
    public interface IState
    {
        public int ArrayIndex { get; set; }

        public int SerializedSize { get; }
    }
}