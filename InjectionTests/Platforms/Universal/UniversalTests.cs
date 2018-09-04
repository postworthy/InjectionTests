using InjectionTests.Common;
using InjectionTests.Extensions;
using InjectionTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace InjectionTests.Platforms.Universal
{
    [TestClass]
    public class UniversalTests<T> where T : class
    {
        private static readonly string[] SQL_KEYWORDS = new[] { "select ", "insert ", "update ", "delete ", "from ", "where ", "order by ", "group by ", "join ", "delete ", "drop ", "set ", "upper ", "lower ", "asc", "desc", "like ", "between " };
        private static readonly string[] LDAP_KEYWORDS = new[] { "ldap", "sid", "objectClass", "extensionAttribute", "ou=", "cn=", "dc=" };
        private readonly InjectionHelper<T> Helper = null;
        public UniversalTests(List<TypeInfo> types = null, List<string> exclusions = null, string endpoint = null, List<SanitizerSearchPattern> additionalSanitizers = null)
        {
            Helper = new InjectionHelper<T>(types, exclusions, endpoint, additionalSanitizers);
        }

        [TestMethod]
        public virtual void SQLi()
        {
            var failures = Helper.TestHelper(x => x.HasDbCommands(), SQL_KEYWORDS, "Potential SQLi");

            if (failures.Length > 0)
                Assert.Fail(Environment.NewLine + string.Join(Environment.NewLine, failures));
        }
        [TestMethod]
        public virtual void FIOi()
        {
            var failures = Helper.TestHelper(x => x.HasFileSystemCommands(), null, "Potential File IO Injection");

            if (failures.Length > 0)
                Assert.Fail(Environment.NewLine + string.Join(Environment.NewLine, failures));
        }
        [TestMethod]
        public virtual void LDAPi()
        {
            var failures = Helper.TestHelper(x => x.HasLdapCommands(), LDAP_KEYWORDS, "Potential LDAPi");

            if (failures.Length > 0)
                Assert.Fail(Environment.NewLine + string.Join(Environment.NewLine, failures));
        }
        [TestMethod]
        public virtual void ASMi()
        {
            var failures = Helper.TestHelper(x => x.HasAssemblyCommands(), null, "Potential Assembly Injection");

            if (failures.Length > 0)
                Assert.Fail(Environment.NewLine + string.Join(Environment.NewLine, failures));
        }
        [TestMethod]
        public virtual void CMDi()
        {
            var failures = Helper.TestHelper(x => x.HasProcessCommands(), null, "Potential Command Injection");

            if (failures.Length > 0)
                Assert.Fail(Environment.NewLine + string.Join(Environment.NewLine, failures));
        }
        [TestMethod]
        public virtual void XXEi()
        {
            var failures = Helper.TestHelper(x => x.HasXXECommands(), null, "Potential XXE Injection");

            if (failures.Length > 0)
                Assert.Fail(Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        public Task RunAll(TextWriter writer)
        {
            return Task.Factory.StartNew(() => {
                try { SQLi(); } catch (Exception ex) { writer.WriteLine(ex.ToString()); }
                try { FIOi(); } catch (Exception ex) { writer.WriteLine(ex.ToString()); }
                try { LDAPi(); } catch (Exception ex) { writer.WriteLine(ex.ToString()); }
                try { ASMi(); } catch (Exception ex) { writer.WriteLine(ex.ToString()); }
                try { CMDi(); } catch (Exception ex) { writer.WriteLine(ex.ToString()); }
                try { XXEi(); } catch (Exception ex) { writer.WriteLine(ex.ToString()); }
            });
        }
    }
}
