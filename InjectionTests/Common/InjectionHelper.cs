using InjectionTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace InjectionTests.Common
{
    public class InjectionHelper<T> : InjectionTests.Common.InjectionTest<T> where T : class
    {
        private readonly string _Endpoint = null;
        private readonly List<string> _Exclusions = null;
        private readonly List<TypeInfo> _Types = null;
        public InjectionHelper(List<TypeInfo> types = null, List<string> exclusions = null, string endpoint = null, List<SanitizerSearchPattern> additionalSanitizers = null)
        {
            _Types = types ?? new List<TypeInfo>();
            _Exclusions = exclusions ?? new List<string>();
            _Endpoint = endpoint;
            if (additionalSanitizers?.Count > 0)
            {
                additionalSanitizers.ForEach(x =>
                {
                    AddSanitizerSearchPattern(x);
                });
            }
        }
        protected override List<TypeInfo> GetReferenceTypes()
        {
            return _Types;
        }
        protected override string GetApiEndpoint()
        {
            return _Endpoint ?? base.GetApiEndpoint();
        }
        protected override List<string> MethodExclusions()
        {
            return _Exclusions == null ? base.MethodExclusions() : _Exclusions.Union(base.MethodExclusions()).ToList();
        }
    }
}
