using System.Reflection;

namespace Test
{
    internal static class ReflectionHelper
    {
        /// <summary>
        /// 设置对象的属性值（包括私有、保护、internal等访问级别）
        /// </summary>
        public static void SetProperty<T>(object obj, string propertyName, T value)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("属性名不能为空", nameof(propertyName));

            var type = obj.GetType();
            var property = type.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null)
            {
                // 在继承链中查找
                property = type.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }

            if (property == null)
                throw new InvalidOperationException($"在类型 {type.FullName} 中找不到属性 {propertyName}");

            if (property.CanWrite)
            {
                // 如果有公共的setter
                property.SetValue(obj, value);
            }
            else
            {
                // 查找私有setter
                var setter = property.GetSetMethod(true);
                if (setter == null)
                    throw new InvalidOperationException($"属性 {propertyName} 没有可用的setter");

                setter.Invoke(obj, new object?[] { value });
            }
        }

        /// <summary>
        /// 获取对象的属性值（包括私有、保护、internal等访问级别）
        /// </summary>
        public static T GetProperty<T>(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("属性名不能为空", nameof(propertyName));

            var type = obj.GetType();
            var property = type.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null)
            {
                property = type.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }

            if (property == null)
                throw new InvalidOperationException($"在类型 {type.FullName} 中找不到属性 {propertyName}");

            if (!property.CanRead)
                throw new InvalidOperationException($"属性 {propertyName} 不可读");

            return (T)property.GetValue(obj)!;
        }
    }
}