﻿using System.Linq;
using Zambon.Core.Module.Interfaces;
using Zambon.Core.Module.Xml;

namespace Zambon.Core.Module.ExtensionMethods
{
    public static class MergeExtension
    {

        public static TObject MergeObject<TObject>(this TObject target, TObject source) where TObject : class, IMergeable
        {
            var properties = target.GetType().GetProperties();
            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].SetMethod != null)
                {
                    var targetValue = properties[i].GetValue(target);
                    var sourceValue = properties[i].GetValue(source);

                    if (targetValue is IMergeable targetValueMergeable && sourceValue is IMergeable sourceValueMergeable)
                    {
                        if (sourceValue != null)
                        {
                            targetValue = targetValueMergeable.MergeObject(sourceValueMergeable);
                            properties[i].SetValue(target, targetValue);
                        }
                    }
                    else if (targetValue != null && targetValue.GetType().IsArray)
                    {
                        targetValue = typeof(MergeExtension).GetMethod("MergeArray").MakeGenericMethod(targetValue.GetType().GetElementType()).Invoke(null, new object[] { targetValue, sourceValue });
                        properties[i].SetValue(target, targetValue);
                    }
                    else
                    {
                        if (targetValue == null || (!targetValue.Equals(sourceValue) && sourceValue != null))
                            properties[i].SetValue(target, sourceValue);
                    }
                }
            }
            return target;
        }

        public static T[] MergeArray<T>(this T[] target, T[] source) where T : class, IMergeable
        {
            if ((source?.Length ?? 0) > 0)
                if ((target?.Length ?? 0) == 0)
                    return source;
                else
                {
                    var key = target.GetType().GetElementType().GetProperties().FirstOrDefault(x => x.CustomAttributes.Any(a => a.AttributeType == typeof(MergeKeyAttribute))).Name;
                    var originalLength = target.Length;

                    var newItems = source.ToList();
                    for (var t = 0; t < originalLength; t++)
                    {
                        var targetItem = target[t];
                        //if (key == string.Empty)
                        //    key = targetItem.GetType().GetProperties().FirstOrDefault(x => x.CustomAttributes.Any(a => a.AttributeType == typeof(MergeKeyAttribute))).Name;

                        var targetKeyValue = targetItem.GetType().GetProperty(key).GetValue(targetItem);

                        //Compare elements that exists in both arrays
                        for (var s = 0; s < newItems.Count; s++)
                        {
                            var sourceItem = newItems[s];
                            var sourceKeyValue = sourceItem.GetType().GetProperty(key).GetValue(sourceItem);

                            if (targetKeyValue.Equals(sourceKeyValue))
                            {
                                s--;
                                newItems.Remove(sourceItem);
                                targetItem.MergeObject(sourceItem);
                            }
                        }
                    }

                    //Add new elements to original array
                    if (newItems.Count > 0)
                        target = target.Union(newItems).ToArray();
                }
            return target;
        }

    }
}