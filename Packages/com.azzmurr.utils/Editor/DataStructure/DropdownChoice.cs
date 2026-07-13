using System;

namespace Azzmurr.Utils {
    internal class DropdownChoice<T> {
        public string Name;
        public Action<T> Action;

        public DropdownChoice(string name, Action<T> action) {
            Name = name;
            Action = action;
        }
    }
}
