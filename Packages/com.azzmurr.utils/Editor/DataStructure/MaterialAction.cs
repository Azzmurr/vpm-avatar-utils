using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    [Serializable]
    public class ActionGroup {
        public string Name { get; set; }
        public List<Button> Actions { get; set; }
    }
}