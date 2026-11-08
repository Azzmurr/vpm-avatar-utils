#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Azzmurr.Utils
{
    [InitializeOnLoad]
    internal static class AnimationWindowGroupByProperty
    {
        private const string HarmonyId = "com.azzmurr.animationwindow.groupbyproperty";
        private const string PrefGroup = "Azzmurr.AnimWindow.GroupByProperty";
        private const string PrefFlatten = "Azzmurr.AnimWindow.FlattenIndent";
        private const string MenuGroup = "Azzmurr/Animation Window/Group by Property Name";
        private const string MenuFlatten = "Azzmurr/Animation Window/Flatten Indent (when grouped)";

        private static bool _sEnabled;
        private static bool _sFlatten;

        private static readonly PropertyInfo SBindingProp;   // AnimationWindowCurve.binding
        private static readonly FieldInfo SIndentField;      // AnimationWindowHierarchyNode.indent
        private static readonly PropertyInfo SWindowState;   // AnimationWindow.state (internal)
        private static readonly PropertyInfo SStateRefresh;  // AnimationWindowState.refresh

        static AnimationWindowGroupByProperty()
        {
            _sEnabled = EditorPrefs.GetBool(PrefGroup, false);
            _sFlatten = EditorPrefs.GetBool(PrefFlatten, true);

            try
            {
                var curveType = AccessTools.TypeByName("UnityEditorInternal.AnimationWindowCurve");
                var dataSourceType = AccessTools.TypeByName("UnityEditorInternal.AnimationWindowHierarchyDataSource");
                var nodeType = AccessTools.TypeByName("UnityEditorInternal.AnimationWindowHierarchyNode");
                var stateType = AccessTools.TypeByName("UnityEditorInternal.AnimationWindowState");

                SBindingProp = AccessTools.Property(curveType, "binding");
                SIndentField = AccessTools.Field(nodeType, "indent");
                SWindowState = AccessTools.Property(typeof(AnimationWindow), "state");
                SStateRefresh = AccessTools.Property(stateType, "refresh");

                var harmony = new Harmony(HarmonyId);

                harmony.Patch(
                    AccessTools.Method(curveType, "CompareTo", new[] { curveType }),
                    prefix: new HarmonyMethod(typeof(AnimationWindowGroupByProperty), nameof(CompareToPrefix)));

                harmony.Patch(
                    AccessTools.Method(dataSourceType, "CreateTreeFromCurves"),
                    postfix: new HarmonyMethod(typeof(AnimationWindowGroupByProperty), nameof(CreateTreePostfix)));
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnimationWindowGroupByProperty] Failed to patch Animation Window: {e}");
            }
        }

        // ---------------------------------------------------------------- patches

        // Original order: path -> type -> property. We put the property *group*
        // (e.g. "material._Color" for .r/.g/.b/.a) first. When groups are equal we let
        // the original run, so within one property the order is path, then r/g/b/a.
        // Equals() is implemented via CompareTo()==0; we only ever return non-zero for
        // curves with different property names, so equality semantics are untouched.
        private static bool CompareToPrefix(object __instance, object obj, ref int __result)
        {
            if (!_sEnabled || obj == null || SBindingProp == null)
                return true;

            var a = GroupName(((EditorCurveBinding)SBindingProp.GetValue(__instance)).propertyName);
            var b = GroupName(((EditorCurveBinding)SBindingProp.GetValue(obj)).propertyName);

            if (string.Equals(a, b, StringComparison.Ordinal))
                return true; // same property -> original ordering by path

            var cmp = EditorUtility.NaturalCompare(a, b);
            if (cmp == 0)
                cmp = string.CompareOrdinal(a, b);

            __result = cmp;
            return false;
        }

        // The hierarchy indents every row by its path depth; with property-first sorting
        // that produces a zig-zag, so optionally flatten it. Rows still show "Object : Prop".
        private static void CreateTreePostfix(object __result)
        {
            if (!_sEnabled || !_sFlatten || SIndentField == null)
                return;

            if (__result is not IList nodes) return;
            foreach (var node in nodes)
                Flatten(node as TreeViewItem);
        }

        private static void Flatten(TreeViewItem item)
        {
            if (item == null)
                return;

            SIndentField.SetValue(item, 0);

            if (item.children == null)
                return;

            foreach (var child in item.children)
                Flatten(child);
        }

        // ---------------------------------------------------------------- helpers

        // Mirrors AnimationWindowUtility.GetPropertyGroupName / GetComponentIndex.
        private static string GroupName(string propertyName) {
            if (propertyName == null || propertyName.Length < 3 || propertyName[^2] != '.')
                return propertyName ?? string.Empty;

            return propertyName[^1] switch {
                'r' or 'g' or 'b' or 'a' or 'x' or 'y' or 'z' or 'w' => propertyName[..^2],
                _ => propertyName
            };
        }

        private static void RefreshAnimationWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<AnimationWindow>())
            {
                try
                {
                    var state = SWindowState?.GetValue(window);
                    if (state != null && SStateRefresh != null)
                    {
                        // RefreshType.Everything = 2 -> clears selection curve caches and rebuilds hierarchy
                        var everything = Enum.ToObject(SStateRefresh.PropertyType, 2);
                        SStateRefresh.SetValue(state, everything);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AnimationWindowGroupByProperty] Refresh failed: {e.Message}");
                }

                window.Repaint();
            }
        }

        // ---------------------------------------------------------------- menu

        [MenuItem(MenuGroup, false, 2000)]
        private static void ToggleGroup()
        {
            _sEnabled = !_sEnabled;
            EditorPrefs.SetBool(PrefGroup, _sEnabled);
            RefreshAnimationWindows();
        }

        [MenuItem(MenuGroup, true)]
        private static bool ToggleGroupValidate()
        {
            Menu.SetChecked(MenuGroup, _sEnabled);
            return true;
        }

        [MenuItem(MenuFlatten, false, 2001)]
        private static void ToggleFlatten()
        {
            _sFlatten = !_sFlatten;
            EditorPrefs.SetBool(PrefFlatten, _sFlatten);
            RefreshAnimationWindows();
        }

        [MenuItem(MenuFlatten, true)]
        private static bool ToggleFlattenValidate()
        {
            Menu.SetChecked(MenuFlatten, _sFlatten);
            return true;
        }
    }
}
#endif
