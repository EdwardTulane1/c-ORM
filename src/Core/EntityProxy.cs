// using Castle.DynamicProxy;

// namespace MyORM.Core
// {
//     //    public class EntityInterceptor : IInterceptor

//     public class EntityInterceptor : IInterceptor
//     {
//         public void Intercept(IInvocation invocation)
//         {
//             // Execute the method first
//             invocation.Proceed();

//             // Check if this is a property setter
//             if (invocation.Method.Name.StartsWith("set_"))
//             {
//                 var propertyName = invocation.Method.Name.Substring(4);
//                 var entity = invocation.InvocationTarget as Entity;
//                 entity?.OnPropertyChanged(propertyName);
//             }

//             // Check if this is a property getter
//             if (invocation.Method.Name.StartsWith("get_"))
//             {
//                 //  here return the real value

                
//             }
//         }
//     }

//     public static class EntityFactory
//     {
//         private static readonly ProxyGenerator _generator = new ProxyGenerator();
//         private static readonly EntityInterceptor _interceptor = new EntityInterceptor();

//         public static T Create<T>() where T : Entity, new()
//         {
//             return _generator.CreateClassProxy<T>(_interceptor);
//         }
//     }
// }