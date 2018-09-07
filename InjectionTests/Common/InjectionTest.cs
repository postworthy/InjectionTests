using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using System.Linq;
using InjectionTests.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using InjectionTests.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Configuration;

namespace InjectionTests.Common
{
    public abstract class InjectionTest<T> where T : class
    {
        private static readonly string[] EXCLUDE = new[] { "mscorlib", "System", "Microsoft", "AjaxControlToolkit", "WebGrease", "DevTrends", "Newtonsoft" };
        protected abstract List<TypeInfo> GetReferenceTypes();
        protected virtual List<string> MethodExclusions()
        {
            return new List<string>();
        }
        protected virtual string GetApiEndpoint()
        {
            return ConfigurationManager.AppSettings["SecurityApiEndpoint"] ?? "";
        }
        protected void AddSanitizerSearchPattern(SanitizerSearchPattern p)
        {
            if(!SanitizerFactory.CustomSanitizerPatterns.Contains(p))
            {
                lock(SanitizerFactory.CustomSanitizerPatterns)
                {
                    if (!SanitizerFactory.CustomSanitizerPatterns.Contains(p))
                        SanitizerFactory.CustomSanitizerPatterns.Add(p);
                }
            }
        }
        public string[] TestHelper(Func<MethodDefinition, bool> inclusionTest, string[] keywords, string warning = "Potential Injection")
        {

            var apiEndpoint = GetApiEndpoint();
            var callEndpoint = !string.IsNullOrEmpty(apiEndpoint);
            var dict = new Dictionary<string, AssemblyDefinition>();
            var resolver = new DefaultAssemblyResolver();
            var types = new List<TypeDefinition>();

            var assemblies = new List<AssemblyDefinition>();

            if (typeof(T) != typeof(object))
            {
                var path = new Uri(typeof(T).Module.Assembly.CodeBase).LocalPath;
                var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(path);
                types.Add(assembly.MainModule.GetType(typeof(T).Namespace, typeof(T).Name));
                dict.Add(path, assembly);
            }

            GetReferenceTypes().ForEach(x =>
            {
                var directory = Path.GetDirectoryName(x.Path);
                var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(x.Path);
                var t = assembly.MainModule.GetType(x.Namespace, x.Name);
                if (t != null)
                {
                    types.Add(t);
                    if (!dict.ContainsKey(x.Path))
                    {
                        dict.Add(x.Path, assembly);
                        resolver.AddSearchDirectory(directory);
                    }
                }
            });

            assemblies = dict.Select(x => x.Value).Where(x => !EXCLUDE.Any(y => x.FullName.StartsWith(y))).Distinct().ToList(); ;

            assemblies.ToArray().ToList().ForEach(asm =>
            {
                assemblies = assemblies.Union(asm.MainModule.AssemblyReferences.Select(x =>
                {
                    try
                    {
                        return resolver.Resolve(x);
                    }
                    catch { return null; }
                }).Where(x => x != null)).ToList();
            });

            assemblies = assemblies.Where(x => !EXCLUDE.Any(y => x.FullName.StartsWith(y))).Distinct().ToList();

            var other = assemblies.SelectMany(asm => asm.MainModule.GetTypes().Where(x => types.Any(t => x.IsSubclassOf(t))));

            var all = other.SelectMany(x => x.Methods).ToList();
            all.AddRange(types.SelectMany(x => x.Methods));

            var filteredMethods = all
                .ExcludeMethods(MethodExclusions())
                .Where(x =>
                //x.IsPublic                                                                                                    
                //&& 
                x.Parameters.Count > 0
                && !x.Parameters.All(y => y.ParameterType.IsValueType)
                && !x.CustomAttributes.Any(y => y.AttributeType.Name.Contains("MarkSafe"))
                ).ToList();

            var failures = new ConcurrentBag<string>();
            var dump = new ConcurrentBag<IEnumerable<IEnumerable<String>>>();

            filteredMethods.AsParallel().ForAll(m =>
            {
                var paths = m.DrillIn(assemblies, methodExclusions: MethodExclusions(), stopWhen: inclusionTest)
                    .GroupBy(x => string.Join("", x)).Select(x => x.First())
                    .Where(x => x.Any(inclusionTest)).ToList();
                var dangeroudPaths = paths
                    .Select(x => new { Path = x.ToList(), DangerousStrings = x.SelectMany(y => y.DangerousStrings().Where(z => true == keywords?.Any(kw => z.ToLower().Contains(kw)))).ToList() })
                    .Where(x=> x.Path.Count == 1 || keywords == null || x.DangerousStrings.Count > 0)
                    .ToList();
                var results = dangeroudPaths.Select(x => x.Path).ToList();
                if (results.Count > 0)
                {
                    foreach (var r in results)
                    {
                        if (callEndpoint)
                            dump.Add(r.Select(x=> new[] { x.Module.FileName, x.FullName }));

                        failures.Add($"{Environment.NewLine}{warning} in: {Environment.NewLine}{string.Join(Environment.NewLine, r.Select((x, y) => $"{string.Join("", Enumerable.Range(0, y).Select(z => "\t"))}{x.DeclaringType.Module.Assembly.Name.Name}::{x.DeclaringType.Name}.{x.Name}"))}");
                    }
                }
            });

            if (callEndpoint && dump.Count > 0)
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.UploadData(apiEndpoint, "POST", System.Text.UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dump.ToArray(), new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })));
                    }
                }
                catch (Exception ex)
                {
                    ex = ex;
                }
            }
            return failures.ToArray();
        }
    }
}
