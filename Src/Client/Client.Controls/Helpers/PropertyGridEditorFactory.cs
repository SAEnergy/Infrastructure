using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Controls
{
    public static class PropertyGridEditorFactory
    {
        //private static Dictionary<Type, Type> RegisteredEditors = new Dictionary<Type, Type>();

        public static PropertyGridEditor GetEditor(Type type)
        {
            //lock (RegisteredEditors)
            //{
            //}

            if (type == typeof(bool)) { return new PropertyGridBoolEditor(); }
            if (type == typeof(TimeSpan)) { return new PropertyGridTimeSpanEditor(); }
            if (type.IsEnum) { return new PropertyGridEnumEditor(); }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) { return new PropertyGridNullableEditor(); }

            return new PropertyGridTextEditor();
        }

        //public static void Register (Type target, Type editor)
        //{
        //    lock (RegisteredEditors)
        //    {
        //        RegisteredEditors.Add(target, editor);
        //    }
        //}
    }
}
