using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
namespace AspNetCore.FileLog
{
    internal class FastExpressions
    {
        private static readonly Regex BackingFieldRegex
            = new Regex("^<([^><]+)>.+Field$", RegexOptions.Compiled);
        static MethodInfo FieldSetValueMethod
            = typeof(FieldInfo).GetMethod("SetValue", new[] { typeof(object), typeof(object) });
        static ConcurrentDictionary<Type, Func<object, string, bool, object, object>> GetterSetters
            = new ConcurrentDictionary<Type, Func<object, string, bool, object, object>>();
        static readonly ConstructorInfo KeyNotFoundExceptionConstructor
            = typeof(KeyNotFoundException).GetConstructor(new Type[] { typeof(string) });
        static readonly MethodInfo ConcatMethod
            = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string), typeof(string) });
        static readonly ConstructorInfo ExceptionConstructor
            = typeof(Exception).GetConstructor(new Type[] { typeof(string), typeof(Exception) });
        static readonly object _state = new object();
        static readonly Type ObjectType = typeof(object);
        Type rootType;
        Type ActionType;
        object CurrentObject;
        protected FastExpressions(object @object)
        {
            CurrentObject = @object;
            rootType = @object.GetType();
            ActionType = Expression.GetActionType(rootType);
        }
        PropertyInfo[] Properties;
        FieldInfo[] Fields;
        List<_SwitchCase> switchCases = new List<_SwitchCase>();

        private Func<object, string, bool, object, object> GenerateExpressions()
        {
            var objectParameter = Expression.Parameter(ObjectType, "@object");
            var nameParameter = Expression.Parameter(typeof(string), "name");
            var isAssignParameter = Expression.Parameter(typeof(bool), "isAssign");
            var valueParameter = Expression.Parameter(ObjectType, "value");


            var currentVariable = Expression.Variable(rootType, "current");
            var instanceVariable = Expression.Variable(ObjectType, "_");


            var messageExpression = Expression.Call(ConcatMethod, Expression.Constant("Cannot found the property or field '"),
                nameParameter, Expression.Constant($"' of '{rootType.FullName}'"));
            var exception = Expression.New(KeyNotFoundExceptionConstructor, messageExpression);

            GenerateSwitchs(currentVariable, instanceVariable, valueParameter);

            var getSwitch = Expression.Switch(nameParameter, Expression.Throw(exception), 
                switchCases.Where(x=>x.IsGetter).Select(x=>x.SwitchCase).ToArray());
            var setSwitch = Expression.Switch(nameParameter, Expression.Throw(exception), 
                switchCases.Where(x => !x.IsGetter).Select(x => x.SwitchCase).ToArray());
            var assign = Expression.IfThen(Expression.NotEqual(valueParameter, Expression.Constant(null)),
                    Expression.Assign(instanceVariable, valueParameter));
            var realVariable = Expression.Assign(currentVariable, Expression.TypeAs(objectParameter, rootType));
            var blockExpressions = new List<Expression> { instanceVariable, assign, realVariable };
            var _if = Expression.IfThenElse(isAssignParameter, setSwitch, getSwitch);
            //blockExpressions.Add(Expression.IfThenElse(isAssignParameter, setSwitch, getSwitch));
            
            var _ex = Expression.Parameter(typeof(Exception),"ex");
            var _messageExpression = Expression.Call(ConcatMethod, Expression.Constant(" Occur error when Get or Set the property or field '"),
               nameParameter, Expression.Constant($"' of '{rootType.FullName}' "));
            var _exception = Expression.New(ExceptionConstructor, _messageExpression, _ex);
            var _try=Expression.TryCatch(Expression.Block(_if),
                Expression.Catch(_ex,Expression.Throw(_exception)));
            blockExpressions.Add(_try);
            blockExpressions.Add(instanceVariable);

            var block = Expression.Block(new ParameterExpression[] {
                        instanceVariable,
                        currentVariable }, blockExpressions);
            var lambda = Expression.Lambda<Func<object, string, bool, object, object>>(block,
                new ParameterExpression[] { objectParameter, nameParameter, isAssignParameter, valueParameter });
            //GetterSetters.TryAdd(type, @delegate);
            //Logger.Information("ReflectExtensions", $"{nameof(FastExpressions)} For Type: {rootType.FullName}{Environment.NewLine}{lambda.GetDebugView()}");
            //Microsoft.EntityFrameworkCore.Metadata.Internal.EntityMaterializerSource
            return lambda.Compile();
        }
        private void  GenerateSwitchs(Expression currentVariable, Expression instanceVariable, Expression valueParameter)
        {
            var pf = GetPropertiesAndFields();
            
            foreach (var p in pf.Properties)
            {
                var _current = currentVariable;
                if (p.DeclaringType != rootType)
                {
                    _current = Expression.TypeAs(currentVariable, p.DeclaringType);
                }
                CreateSwitchForProperty(p.DeclaringType, p, _current, instanceVariable, valueParameter);
            }
            foreach (var fi in pf.Fields)
            {
                if (BackingFieldRegex.IsMatch(fi.Name))
                {
                    continue;
                }
                var _current = currentVariable;
                if (fi.DeclaringType != rootType)
                {
                    _current = Expression.TypeAs(currentVariable, fi.DeclaringType);
                }
                CreateSwitchForField(fi.DeclaringType, fi, _current, instanceVariable, valueParameter);
            }
        }
        private (PropertyInfo[] Properties, FieldInfo[] Fields) GetPropertiesAndFields()
        {
            var pis = rootType.GetRuntimeProperties().ToList();
            var fis = rootType.GetRuntimeFields().ToList();
            var _type = rootType.BaseType;
            while (_type != null)
            {
                var _pis = _type.GetRuntimeProperties();
                foreach (var _p in _pis)
                {
                    if (pis.Any(x => !x.Name.Equals(_p.Name)))
                    {
                        pis.Add(_p);
                    }
                }
                var _fis = _type.GetRuntimeFields();
                foreach (var _f in _fis)
                {
                    if (fis.Any(x => !x.Name.Equals(_f.Name)))
                    {
                        fis.Add(_f);
                    }
                }
                _type = _type.BaseType;
            }
            Properties = pis.ToArray();
            Fields = fis.ToArray();
            return (Properties, Fields);
        }
        private void CreateSwitchForProperty(Type type, PropertyInfo pi, Expression currentVariable,
            Expression instanceVariable, Expression valueParameter)
        {
            FieldInfo field = null;
            if (pi.CanWrite)
            {
                //if (pi.SetMethod.GetParameters().Length > 0)
                //{
                //    return;
                //}
                var member = Expression.Property(pi.SetMethod.IsStatic ? null : currentVariable, pi);
                var assign = Expression.Assign(member, Expression.Convert(valueParameter, pi.PropertyType));
                AddSwitch(Expression.Block(assign, Expression.Empty()), pi.Name, false);
            }
            else
            {
                field = Fields.FirstOrDefault(fi => Regex.IsMatch(fi.Name, $"^<{pi.Name}>.+Field$"));
                if (field != null)
                {
                    if (field.IsInitOnly)
                    {
                        var call = Expression.Call(Expression.Constant(field), FieldSetValueMethod, currentVariable, valueParameter);
                        AddSwitch(call, pi.Name, false);
                    }
                    else
                    {
                        var member = Expression.Field(field.IsStatic ? null : currentVariable, field);
                        var assign = Expression.Assign(member, Expression.Convert(valueParameter, pi.PropertyType));
                        AddSwitch(Expression.Block(assign, Expression.Empty()), pi.Name, false);
                    }
                }
            }
            if (pi.CanRead)
            {
                //if (pi.GetMethod.GetParameters().Length > 0)
                //{
                //    return;
                //}
                var assign = Expression.Assign(instanceVariable,
                    Expression.Convert(Expression.Property(pi.GetMethod.IsStatic ? null : currentVariable, pi), ObjectType));
                AddSwitch(Expression.Block(assign, Expression.Empty()), pi.Name, true);
            }
            else
            {
                if (field != null)
                {
                    var assign = Expression.Assign(instanceVariable,
                        Expression.Convert(Expression.Field(field.IsStatic ? null : currentVariable, field), ObjectType));
                    AddSwitch(Expression.Block(assign, Expression.Empty()), pi.Name, true);
                }
            }

        }
        private void CreateSwitchForField(Type type, FieldInfo field, Expression currentVariable,
           Expression instanceVariable, Expression valueParameter)
        {
            if (field.IsLiteral)
            {
                return;
            }
            if (field.IsInitOnly)
            {
                var call = Expression.Call(Expression.Constant(field), FieldSetValueMethod, currentVariable, valueParameter);
                AddSwitch(call, field.Name, false);
            }
            else
            {
                var member = Expression.Field(field.IsStatic ? null : currentVariable, field);
                var assign = Expression.Assign(member, Expression.Convert(valueParameter, field.FieldType));
                AddSwitch(Expression.Block(assign, Expression.Empty()), field.Name, false);
            }
            var _assign = Expression.Assign(instanceVariable,
               Expression.Convert(Expression.Field(field.IsStatic ? null : currentVariable, field), ObjectType));
            AddSwitch(Expression.Block(_assign, Expression.Empty()), field.Name, true);
        }
        void AddSwitch(Expression expr, string name, bool isGetter)
        {
            if (!switchCases.Any(x => x.IsGetter && x.Name == name))
            {              
                switchCases.Add(new _SwitchCase
                {
                    SwitchCase = Expression.SwitchCase(expr,Expression.Constant(name)),
                    IsGetter = isGetter,
                    Name = name                    
                });
            }
        }
        class _SwitchCase
        {
            public SwitchCase SwitchCase { get; set; }
            public bool IsGetter { get; set; }
            public string Name { get; set; }
        }

        public static Func<object, string, bool, object, object> CreateDelegate(object @object)
        {
            if (@object == null)
            {
                return null;
            }
            var type = @object.GetType();
            return GetterSetters.GetOrAdd(type, _func =>
            {
                var exp = new FastExpressions(@object);
                return exp.GenerateExpressions();
            });
        }
    }
}
