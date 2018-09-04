using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InjectionTests.Extensions
{
    static internal class TypeDefinitionExtensions
    {
        /// <summary>
        /// Is childTypeDef a subclass of parentTypeDef. Does not test interface inheritance
        /// </summary>
        /// <param name="childTypeDef"></param>
        /// <param name="parentTypeDef"></param>
        /// <returns></returns>
        public static bool IsSubclassOf(this TypeDefinition childTypeDef, TypeDefinition parentTypeDef) =>
           childTypeDef.MetadataToken
               != parentTypeDef.MetadataToken
               && childTypeDef
              .EnumerateBaseClasses()
              .Any(b => b.MetadataToken == parentTypeDef.MetadataToken);

        /// <summary>
        /// Enumerate the current type, it's parent and all the way to the top type
        /// </summary>
        /// <param name="klassType"></param>
        /// <returns></returns>
        public static IEnumerable<TypeDefinition> EnumerateBaseClasses(this TypeDefinition klassType)
        {
            for (var typeDefinition = klassType; typeDefinition != null; typeDefinition = typeDefinition.BaseType?.TryResolve())
            {
                yield return typeDefinition;
            }
        }

        public static TypeDefinition TryResolve(this TypeReference t)
        {
            try
            {
                return t.Resolve();
            }
            catch { return null; }
        }
    }
}
