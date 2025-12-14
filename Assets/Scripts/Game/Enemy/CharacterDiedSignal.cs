// Assets/_Project/Code/Runtime/Core/Signals/CharacterDiedSignal.cs
using UnityEngine;

namespace Fogoyote.BFLike.Core.Signals
{
    public struct CharacterDiedSignal
    {
        public GameObject Who;
        public CharacterDiedSignal(GameObject who) { Who = who; }
    }
}
