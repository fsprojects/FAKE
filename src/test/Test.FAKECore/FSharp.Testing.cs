using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;

namespace FSharp.Testing
{
    public class TargetInformation<T>
    {
        public TargetInformation(T target, PropertyInfo property)
        {
            Target = target;
            Property = property;
        }

        public T Target { get; private set; }
        public PropertyInfo Property { get; private set; }
    }

    public static class ReflectionExtensions
    {
        /// <summary>
        ///   Gets the name of the property.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <typeparam name = "TProperty">The type of the property.</typeparam>
        /// <param name = "expression">The expression.</param>
        /// <returns></returns>
        public static string GetPropertyName<T, TProperty>(this Expression<Func<T, TProperty>> expression)
        {
            return GetProperty(expression).Name;
        }

        /// <summary>
        ///   Gets the property.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <typeparam name = "TProperty">The type of the property.</typeparam>
        /// <param name = "expression">The expression.</param>
        /// <returns></returns>
        public static PropertyInfo GetProperty<T, TProperty>(this Expression<Func<T, TProperty>> expression)
        {
            return (PropertyInfo) ((MemberExpression) expression.Body).Member;
        }

        /// <summary>
        ///   Copies the record.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "record">The record.</param>
        /// <returns></returns>
        public static T CopyRecord<T>(this T record)
        {
            return CreateModifiedCopy<T, object>(record, null, null);
        }

        /// <summary>
        ///   Creates a modified copy of the original object.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <typeparam name = "TProperty">The type of the property.</typeparam>
        /// <param name = "record">The record.</param>
        /// <param name = "property">Name of the property.</param>
        /// <param name = "value">The value.</param>
        /// <returns></returns>
        static T CreateModifiedCopy<T, TProperty>(T record, PropertyInfo property, TProperty value)
        {
            var originalValues =
                FSharpType.GetRecordFields(typeof (T), null)
                    .Select(p => new{ X = p, Y = FSharpValue.GetRecordField(record, p)});

            var values =
                originalValues
                    .Select(t => t.X == property ? value : t.Y)
                    .ToArray();

            return (T) FSharpValue.MakeRecord(typeof (T), values, FSharpOption<BindingFlags>.None);
        }

        /// <summary>
        ///   Extracts the property name and creates a targetInformation with target object and backing field reference.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <typeparam name = "TProperty"></typeparam>
        /// <param name = "target">The target.</param>
        /// <param name = "expression">The expression.</param>
        /// <returns></returns>
        public static TargetInformation<T> Set<T, TProperty>(this T target, Expression<Func<T, TProperty>> expression)
        {
            return new TargetInformation<T>(target, expression.GetProperty());
        }

        /// <summary>
        ///   Takes a targetInformation with target object and backing field reference and sets the given value on it.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <typeparam name = "TProperty"></typeparam>
        /// <param name = "targetInformation">The targetInformation.</param>
        /// <param name = "value">The value.</param>
        /// <returns></returns>
        public static T To<T, TProperty>(this TargetInformation<T> targetInformation, TProperty value)
        {
            return CreateModifiedCopy(targetInformation.Target, targetInformation.Property, value);
        }

        /// <summary>
        ///   Extracts the property name and sets the given value on it.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <typeparam name = "TProperty"></typeparam>
        /// <param name = "target">The target.</param>
        /// <param name = "expression">The expression.</param>
        /// <param name = "value">The value.</param>
        /// <returns></returns>
        public static T With<T, TProperty>(this T target, Expression<Func<T, TProperty>> expression, TProperty value)
        {
            return target.Set(expression).To(value);
        }
    }

    public class BackingFields
    {
        /// <summary>
        ///   Gets the backing field for the given property.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "propertyName">Name of the property.</param>
        /// <returns></returns>
        public static FieldInfo GetBackingField<T>(string propertyName)
        {
            return GetBackingField(typeof(T), propertyName);
        }

        /// <summary>
        /// Gets the backing field for a given property name.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public static FieldInfo GetBackingField(Type type, string propertyName)
        {
            // TODO: Apply Open-Closed-Principle
            var fieldName = propertyName + "@";
            var field = GetPrivateFieldInfo(type, fieldName);

            if (field == null)
            {
                fieldName = String.Format("<{0}>k__BackingField", propertyName);
                field = GetPrivateFieldInfo(type, fieldName);
            }

            if (field == null)
            {
                fieldName = propertyName + "Field";
                field = GetPrivateFieldInfo(type, fieldName);
            }
            if (field == null)
                throw new Exception(String.Format("Backing field for property {0} could not be found.", propertyName));
            return field;
        }

        static FieldInfo GetPrivateFieldInfo(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}