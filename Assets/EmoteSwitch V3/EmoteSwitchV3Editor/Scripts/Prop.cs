using UnityEngine;

namespace Gatosyocora.EmoteSwitchV3Editor
{
    /// <summary>
    /// EmoteSwitchV3で操作するオブジェクト
    /// </summary>
    public class Prop
    {
        public GameObject Obj { get; set; }
        public bool DefaultState { get; set; } = false;
        public bool IsLocalEmoteSwitch { get; set; } = false;

        public Prop(GameObject obj = null)
        {
            this.Obj = obj;
            DefaultState = false;
            IsLocalEmoteSwitch = false;
        }
    }
}

