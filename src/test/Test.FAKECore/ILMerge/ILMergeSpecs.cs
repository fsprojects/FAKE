using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.ILMerge
{
    public static class FSharpRecordExtensions
    {
        public static string GetPropertyName<T, TProperty>(this Expression<Func<T, TProperty>> expression)
        {
            return ((MemberExpression) expression.Body).Member.Name;
        }

        public static Tuple<T, FieldInfo> Set<T, S>(this T target, Expression<Func<T, S>> expression)
        {
            var propertyName = expression.GetPropertyName();
            var fieldName = propertyName + "@";
            var field = typeof (T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new Exception(
                    string.Format("Backing field {0} for property {1} could not be found.",
                                  fieldName, propertyName));
            return Tuple.Create(target, field);
        }

        public static T To<T, S>(this Tuple<T, FieldInfo> tuple, S value)
        {
            tuple.Item2.SetValue(tuple.Item1, value);
            return tuple.Item1;
        }

        public static T With<T, S>(this T target, Expression<Func<T, S>> expression, S value)
        {
            return target.Set(expression).To(value);
        }
    }

    public class when_creating_ILMerge_default_arguments
    {
        const string OutputDll = @".\build\myoutput.dll";
        const string PrimaryAssembly = "myPrimaryAssembly.dll";
        static ILMergeHelper.ILMergeParams _parameters;
        static string _arguments;

        Establish context = () =>
            {
                _parameters =
                    ILMergeHelper.ILMergeDefaults
                        .Set(p => p.Closed).To(true)
                        .Set(p => p.CopyAttributes).To(false);
                Trace.WriteLine(_parameters.ToString());
            };

        Because of =
            () => _arguments = ILMergeHelper.getArguments(OutputDll, PrimaryAssembly, _parameters);


        It should_have_the_right_arguments = () => _arguments.ShouldEqual("/out:\".\\build\\myoutput.dll\" /target:\"fake.ilmergehelper+targetkind\" /closed myPrimaryAssembly.dll");
    }
}