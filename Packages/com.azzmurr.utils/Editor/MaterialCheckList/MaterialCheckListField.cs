using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    public class MaterialCheckListField : VisualElement {
        private readonly ListView _itemsListView;

        private readonly TextField _shaderNameField;
        private SerializedProperty _behaviour; // MaterialCheckListBehaviour
        private SerializedProperty _items; // MaterialCheckListItem[]
        private SerializedProperty _shaderName; // string

        public MaterialCheckListField() {
            _shaderNameField = new TextField { label = "Shader Name" };

            _itemsListView = new ListView {
                name = "Check List",
                showAddRemoveFooter = true,
                reorderable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                headerTitle = "Check List",
                showFoldoutHeader = true,
                reorderMode = ListViewReorderMode.Animated,
                showBoundCollectionSize = true,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = {
                    marginTop = 8,
                },
            };

            _itemsListView.makeItem += () => new MaterialCheckListItemField();
            _itemsListView.bindItem += BindParameterListItem;

            _itemsListView.itemsAdded += _ => { };
            _itemsListView.itemsRemoved += _ => { };

            Add(_shaderNameField);
            Add(_itemsListView);
        }

        private void BindParameterListItem(VisualElement element, int i) {
            var parameterField = (MaterialCheckListItemField)element;
            var param = _items.GetArrayElementAtIndex(i);
            parameterField.BindProperty(param);
        }

        public void BindProperty(SerializedProperty prop) {
            _behaviour = prop;

            _shaderName = _behaviour.FindPropertyRelative(nameof(MaterialCheckListBehaviour.shaderName));
            _items = _behaviour.FindPropertyRelative(nameof(MaterialCheckListBehaviour.items));

            _shaderNameField.BindProperty(_shaderName);
            _itemsListView.BindProperty(_items);
        }
    }
}