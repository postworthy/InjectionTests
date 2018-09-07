using InjectionTests.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InjectionTests.Extensions
{
    public static class Cecil
    {
        private static readonly object locker = new object();
        private static Dictionary<string, List<MethodDefinition>> previous = new Dictionary<string, List<MethodDefinition>>();
        private static Dictionary<MethodDefinition, List<string>> Cache = new Dictionary<MethodDefinition, List<string>>();
        private static List<MethodDefinition> GetAllMethods(List<AssemblyDefinition> assemblies)
        {
            var key = string.Join(",", assemblies.Select(x => x.FullName).OrderBy(x => x));
            if (!previous.ContainsKey(key))
            {
                lock (locker)
                {
                    if (!previous.ContainsKey(key))
                        previous.Add(key, assemblies
                            .SelectMany(a => a.MainModule.GetTypes().SelectMany(x => x.Methods))
                            .Where(m => !m.CustomAttributes.Any(x => x.AttributeType.Name == "MarkSafeAttribute"))
                            .ToList());
                }
            }

            return previous[key];
        }
        public static IEnumerable<IEnumerable<MethodDefinition>> DrillIn(this MethodDefinition method, List<AssemblyDefinition> assemblies, IEnumerable<string> methodExclusions = null, int stackDepth = 0, Func<MethodDefinition, bool> stopWhen = null)
        {
            if ((stopWhen == null || !stopWhen(method)))
            {
                var instructions = method?.Body?.Instructions?.Select(x => (x.Operand as MethodReference)).Where(x => x != null).ToList() ?? new List<MethodReference>();

                var methods = GetAllMethods(assemblies);

                foreach (var i in instructions)
                {
                    if (i.Parameters.Count > 0 && !i.Parameters.All(y => y.ParameterType.IsValueType))
                    {
                        foreach (var m in methods.Where(x => x != method))
                        {
                            if ((i.Name == m.Name && i.FullName == m.FullName) &&
                                (m.Parameters.Count > 0 && !m.Parameters.All(y => y.ParameterType.IsValueType || (y.ParameterType.IsGenericInstance && ((GenericInstanceType)y.ParameterType).GenericArguments.All(z => z.IsValueType)))) &&
                                (methodExclusions == null || !methodExclusions.Any(x => m.FullName.Split(' ')[1].Split('(')[0] == x)))
                            {
                                if (stackDepth < 3 && (stopWhen == null || !stopWhen(m)))
                                {
                                    var results = m.DrillIn(assemblies, methodExclusions, stackDepth + 1, stopWhen);
                                    foreach (var r in results)
                                    {
                                        var x = new List<MethodDefinition>(r);
                                        x.Insert(0, method);
                                        yield return x;
                                    }
                                }
                                else
                                    yield return new[] { method, m };
                            }
                        }
                    }
                }
            }
            else
                yield return new[] { method };
        }

        public static IEnumerable<MethodDefinition> ExcludeMethods(this IEnumerable<MethodDefinition> methods, IEnumerable<string> methodExclusions)
        {

            foreach (var method in methods)
            {
                if (methodExclusions != null && methodExclusions.Any(m => method.FullName.Split(' ')[1].Split('(')[0] == m))
                    continue;

                yield return method;
            }
        }

        public static List<List<MethodDefinition>> TrimEnd(this IEnumerable<IEnumerable<MethodDefinition>> chains, Func<MethodDefinition, bool> shouldTrim)
        {
            var ret = new ConcurrentBag<List<MethodDefinition>>();
            Parallel.ForEach(chains, c =>
            {
                var trimmed = c.ToList();
                trimmed.Reverse();
                var take = trimmed.Count;
                var didBreak = false;
                for (int i = 0; i < trimmed.Count; i++)
                {
                    if (shouldTrim(trimmed[i]))
                        take--;
                    else
                    {
                        didBreak = true;
                        break;
                    }

                }
                if (didBreak)
                {
                    trimmed.Reverse();
                    ret.Add(trimmed.Take(take).ToList());
                }
            });
            return ret.GroupBy(x => string.Join("", x)).Select(x => x.First()).ToList();
        }
        
        public static List<string> DangerousStrings(this MethodDefinition method)
        {
            if (!Cache.ContainsKey(method))
            {
                lock (Cache)
                {
                    if (!Cache.ContainsKey(method))
                        Cache.Add(method, method.DangerousStringsInternal().ToList());
                }
            }

            return Cache[method];
        }

        #region PRIVATE HELPERS
        private static IEnumerable<string> DangerousStringsInternal(this MethodDefinition method)
        {
            foreach (var c in method.StringConstants(new Regex("{[0-9]}", RegexOptions.Compiled)))
            {
                yield return c;
            }

            foreach (var c in method.StringConstantsFromConcat())
            {
                yield return c;
            }

            foreach (var c in method.StringConstantsFromAppend())
            {
                yield return c;
            }

            foreach (var c in method.WebControlInputs())
            {
                yield return c;
            }
        }

        private static bool HasVariableStringFormats(this MethodDefinition method)
        {
            var instructions = method?.Body?.Instructions?.Where(x => (x.Operand as MethodReference)?.Name == "Format")?.ToList() ?? new List<Instruction>();

            foreach (var inst in instructions)
            {
                var x = inst;
                //Doesn't properly handle Object[] or IFormatProvider based String.Format method signatures but the failure case will not exclude but include
                switch ((inst.Operand as MethodReference).Parameters.Count)
                {
                    case 2:
                        if (x.Previous.OpCode != OpCodes.Ldstr)
                            return true;
                        break;
                    case 3:
                        if (x.Previous.OpCode != OpCodes.Ldstr && x.Previous.Previous.OpCode != OpCodes.Ldstr)
                            return true;
                        break;
                    case 4:
                        if (x.Previous.OpCode != OpCodes.Ldstr && x.Previous.Previous.OpCode != OpCodes.Ldstr && x.Previous.Previous.Previous.OpCode != OpCodes.Ldstr)
                            return true;
                        break;
                }
            }

            return false;
        }

        private static IEnumerable<string> StringConstants(this MethodDefinition method, Regex regex = null, IEnumerable<string> stringExclusions = null)
        {
            var strings = new List<string>();

            if (method.HasVariableStringFormats())
            {
                strings = method?.Body?.Instructions
                    ?.Where(x => x.OpCode == OpCodes.Ldstr)
                    ?.Where(x=> InjectionTests.Models.SanitizerFactory.Create().Where(y=>y.SearchType == Models.SanitizerSearchPattern.Type.Format || y.SearchType == Models.SanitizerSearchPattern.Type.All).All(y=>y.IsNotMatch(x)))
                    //?.Where(x => !x.Nearest("format", InstructionExtensions.Direction.Next, 10).IsNearMethod("parameter", InstructionExtensions.Direction.Next, 5))
                    //?.Where(x => !x.IsNearMethod("log", InstructionExtensions.Direction.Previous))
                    //?.Where(x => !x.Nearest("format", InstructionExtensions.Direction.Next, 10).IsNearMethod("log", InstructionExtensions.Direction.Next, 5))
                    //?.Where(x => !x.Nearest("format", InstructionExtensions.Direction.Next, 10).IsNearMethod("get_item", InstructionExtensions.Direction.Previous))
                    //?.Where(x => !(x.Nearest("format", InstructionExtensions.Direction.Next, 10).Nearest(".ctor", InstructionExtensions.Direction.Next, 5)?.Operand?.ToString() == "System.Void System.Exception::.ctor(System.String)"))
                    //?.Where(x => !(x.Nearest("format", InstructionExtensions.Direction.Next, 10).Nearest(".ctor", InstructionExtensions.Direction.Next, 5)?.Operand?.ToString() == "System.Void System.Security.SecurityException::.ctor(System.String)"))
                    //?.Where(x => !(x.Nearest("get_response", InstructionExtensions.Direction.Previous)?.Operand?.ToString()?.StartsWith("System.Web.HttpResponse ") == true))
                    ?.Where(x => regex.IsMatch(x.Operand.ToString()))
                    ?.Select(x => x.Operand.ToString())
                    ?.Except(stringExclusions ?? new[] { "" })
                    ?.ToList() ?? new List<string>();
            }

            return strings;
        }

        private static IEnumerable<string> StringConstantsFromConcat(this MethodDefinition method, IEnumerable<string> stringExclusions = null)
        {
            var strings = new List<string>();

            var concats = method?.Body?.Instructions
                    ?.Where(x => x.OpCode == OpCodes.Ldstr)
                    ?.Where(x => InjectionTests.Models.SanitizerFactory.Create().Where(y => y.SearchType == Models.SanitizerSearchPattern.Type.Concat || y.SearchType == Models.SanitizerSearchPattern.Type.All).All(y => y.IsNotMatch(x)))
                    ?.Select(x => new { x, concat = x.Nearest("concat", InstructionExtensions.Direction.Next, 10) })
                    ?.Select(x => new { x.x, x.concat, method = x.concat.Operand as MethodReference });
                    //?.Where(x => x.concat.OpCode != OpCodes.Nop && x.method != null)
                    //?.Where(x => !x.concat.IsNearMethod("parameter", InstructionExtensions.Direction.Next, 5))
                    //?.Where(x => !x.concat.IsNearMethod("log", InstructionExtensions.Direction.Next, 5))
                    //?.Where(x => !x.x.IsNearMethod("log", InstructionExtensions.Direction.Previous))
                    //?.Where(x => !x.x.IsNearMethod("get_item", InstructionExtensions.Direction.Next))
                    //?.Where(x => !(x.concat.Nearest(".ctor", InstructionExtensions.Direction.Next, 5)?.Operand?.ToString() == "System.Void System.Exception::.ctor(System.String)"))
                    //?.Where(x => !(x.concat.Nearest(".ctor", InstructionExtensions.Direction.Next, 5)?.Operand?.ToString() == "System.Void System.Security.SecurityException::.ctor(System.String)"))
                    //?.Where(x => !(x.x.Nearest("get_response", InstructionExtensions.Direction.Previous)?.Operand?.ToString()?.StartsWith("System.Web.HttpResponse ") == true));

            if (true == concats?.All(x => x.concat.Take(x.method.Parameters.Count, InstructionExtensions.Direction.Previous).All(y => y.OpCode != OpCodes.Ldstr && !y.OpCode.Name.ToLower().StartsWith("ldloc"))))
            {
                strings = concats
                        ?.Select(x => x.x.Operand.ToString())
                        ?.Except(stringExclusions ?? new[] { "" })
                        ?.ToList() ?? new List<string>();
            }

            return strings;
        }

        private static IEnumerable<string> StringConstantsFromAppend(this MethodDefinition method, IEnumerable<string> stringExclusions = null)
        {
            var strings = new List<string>();

            var appends = method?.Body?.Instructions
                    ?.Where(x => x.OpCode == OpCodes.Ldstr)
                    ?.Where(x => InjectionTests.Models.SanitizerFactory.Create().Where(y => y.SearchType == Models.SanitizerSearchPattern.Type.Append || y.SearchType == Models.SanitizerSearchPattern.Type.All).All(y => y.IsNotMatch(x)))
                    ?.Select(x => new { x, append = x.Nearest("append", InstructionExtensions.Direction.Next, 10) })
                    ?.Select(x => new { x.x, x.append, method = x.append.Operand as MethodReference });
                    //?.Where(x => x.append.OpCode != OpCodes.Nop && x.method != null)
                    //?.Where(x => !x.append.IsNearMethod("parameter", InstructionExtensions.Direction.Next, 5))
                    //?.Where(x => !x.append.IsNearMethod("log", InstructionExtensions.Direction.Next, 5))
                    //?.Where(x => !x.x.IsNearMethod("log", InstructionExtensions.Direction.Previous))
                    //?.Where(x => !x.x.IsNearMethod("get_item", InstructionExtensions.Direction.Next))
                    //?.Where(x => !(x.append.Nearest(".ctor", InstructionExtensions.Direction.Next, 5)?.Operand?.ToString() == "System.Void System.Exception::.ctor(System.String)"))
                    //?.Where(x => !(x.append.Nearest(".ctor", InstructionExtensions.Direction.Next, 5)?.Operand?.ToString() == "System.Void System.Security.SecurityException::.ctor(System.String)"))
                    //?.Where(x => !(x.x.Nearest("get_response", InstructionExtensions.Direction.Previous)?.Operand?.ToString()?.StartsWith("System.Web.HttpResponse ") == true));

            if (!(true == appends?.All(x => x.append.Take(x.method.Parameters.Count, InstructionExtensions.Direction.Previous).All(y => y.OpCode != OpCodes.Ldstr && !y.OpCode.Name.ToLower().StartsWith("ldloc")))))
            {
                strings = appends
                        ?.Select(x => x.x.Operand.ToString())
                        ?.Except(stringExclusions ?? new[] { "" })
                        ?.ToList() ?? new List<string>();
            }

            return strings;
        }

        private static IEnumerable<string> WebControlInputs(this MethodDefinition method, IEnumerable<string> stringExclusions = null)
        {
            var strings = new List<string>();

            strings = method?.Body?.Instructions
                ?.Where(x => x.Operand?.ToString()?.Contains("System.Web.UI.WebControls") == true && (x.Operand?.ToString()?.ToLower()?.Contains("::get_text") == true || x.Operand?.ToString()?.ToLower()?.Contains("::get_value") == true || x.Operand?.ToString()?.ToLower()?.Contains("::get_selectedvalue") == true))
                ?.Select(x => x.Operand.ToString())
                ?.Except(stringExclusions ?? new[] { "" })
                ?.ToList() ?? new List<string>();

            return strings;
        }
        #endregion
    }
}
