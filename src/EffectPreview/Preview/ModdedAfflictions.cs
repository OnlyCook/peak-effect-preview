using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Peak.Afflictions;

namespace EffectPreview.Preview
{
    // reads vanilla status effects out of afflictions another mod defined, without knowing the mod
    internal static class ModdedAfflictions
    {
        private const string OverTimePrefix = "Affliction_Adjust";
        private const string OverTimeSuffix = "OverTime";
        private const string PerSecondFieldName = "statusPerSecond";
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly Assembly GameAssembly = typeof(Affliction).Assembly;
        private static readonly Dictionary<Type, Shape> Shapes = new Dictionary<Type, Shape>();

        private class Shape
        {
            internal readonly List<FieldInfo> NestedFields = new List<FieldInfo>();
            internal FieldInfo PerSecondField;
            internal CharacterAfflictions.STATUSTYPE OverTimeStatus;
        }

        internal static bool IsModded(Affliction affliction)
        {
            return affliction != null && affliction.GetType().Assembly != GameAssembly;
        }

        // a modded copy of vanilla's Affliction_AdjustColdOverTime pattern for another vanilla status
        internal static bool TryGetStatusOverTime(Affliction affliction, out CharacterAfflictions.STATUSTYPE type, out float total)
        {
            Shape shape = GetShape(affliction.GetType());
            type = shape.OverTimeStatus;
            total = 0f;
            if (shape.PerSecondField == null)
            {
                return false;
            }
            total = (float)shape.PerSecondField.GetValue(affliction) * affliction.totalTime;
            return true;
        }

        // vanilla afflictions a modded wrapper holds in its own fields and applies itself
        internal static void CollectNested(Affliction affliction, List<Affliction> into)
        {
            foreach (FieldInfo field in GetShape(affliction.GetType()).NestedFields)
            {
                object value = field.GetValue(affliction);
                if (value is Affliction nested)
                {
                    into.Add(nested);
                }
                else if (value is IEnumerable sequence)
                {
                    foreach (object element in sequence)
                    {
                        if (element is Affliction nestedElement)
                        {
                            into.Add(nestedElement);
                        }
                    }
                }
            }
        }

        private static Shape GetShape(Type type)
        {
            if (Shapes.TryGetValue(type, out Shape shape))
            {
                return shape;
            }
            shape = new Shape();
            for (Type t = type; t != null && t != typeof(Affliction); t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(InstanceFields))
                {
                    if (IsAfflictionHolder(field.FieldType))
                    {
                        shape.NestedFields.Add(field);
                    }
                }
            }

            string name = type.Name;
            if (name.StartsWith(OverTimePrefix, StringComparison.Ordinal) && name.EndsWith(OverTimeSuffix, StringComparison.Ordinal)
                && name.Length > OverTimePrefix.Length + OverTimeSuffix.Length
                && Enum.TryParse(name.Substring(OverTimePrefix.Length, name.Length - OverTimePrefix.Length - OverTimeSuffix.Length), true, out CharacterAfflictions.STATUSTYPE status)
                && status != CharacterAfflictions.STATUSTYPE.Petrify)
            {
                FieldInfo perSecond = type.GetField(PerSecondFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (perSecond != null && perSecond.FieldType == typeof(float))
                {
                    shape.PerSecondField = perSecond;
                    shape.OverTimeStatus = status;
                }
            }

            Shapes[type] = shape;
            return shape;
        }

        private static bool IsAfflictionHolder(Type fieldType)
        {
            if (typeof(Affliction).IsAssignableFrom(fieldType))
            {
                return true;
            }
            if (fieldType.IsArray)
            {
                return typeof(Affliction).IsAssignableFrom(fieldType.GetElementType());
            }
            return fieldType.IsGenericType && fieldType.GetGenericArguments().Length == 1
                && typeof(Affliction).IsAssignableFrom(fieldType.GetGenericArguments()[0])
                && typeof(IEnumerable).IsAssignableFrom(fieldType);
        }
    }
}
