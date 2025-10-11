using System;

namespace EasyParkingAPI.Tools
{
    public static class Tools
    {
        public static string ExceptionMessage(Exception e)
        {
            string message = e.Message;
            while (e.InnerException != null)
            {
                e = e.InnerException;
                message = message + " -*- " + e.Message;
            }
            ;


            return message;

        }

        public class PropertyCopier<TParent, TChild> where TParent : class // COPIA PROPEDADES DE UN OBJETO A OTRO SI SON SIMILARES
                                             where TChild : class
        {
            public static TChild Copy(TParent parent, TChild child)
            {
                var parentProperties = parent.GetType().GetProperties();
                var childProperties = child.GetType().GetProperties();

                foreach (var parentProperty in parentProperties)
                {
                    foreach (var childProperty in childProperties)
                    {
                        if (parentProperty.Name == childProperty.Name && parentProperty.PropertyType == childProperty.PropertyType)
                        {
                            childProperty.SetValue(child, parentProperty.GetValue(parent));
                            break;
                        }
                    }
                }

                return child;
            }
        }
    }


}
