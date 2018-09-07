using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InjectionTests.Models
{
    public static class SanitizerFactory
    {
        internal static List<SanitizerSearchPattern> CustomSanitizerPatterns { get; private set; } = new List<SanitizerSearchPattern>();
        public static IEnumerable<SanitizerSearchPattern> Create()
        {
            var results = CustomSanitizerPatterns.Union(CreateFormatSearchPatterns()).Union(CreateConcatSearchPatterns()).Union(CreateAppendSearchPatterns()).Distinct().ToList();
            return results;
        }
        private static IEnumerable<SanitizerSearchPattern> CreateFormatSearchPatterns()
        {
            var patterns = new List<SanitizerSearchPattern>
            {
                new SanitizerSearchPattern()
                {
                    Pattern = "Format",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = "Parameter",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Log",
                    Steps = 1,
                    Direction = Extensions.InstructionExtensions.Direction.Previous
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Format",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = "Log",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Format",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = "Get_Item",
                        Steps = 1,
                        Direction = Extensions.InstructionExtensions.Direction.Previous
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Format",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = ".ctor",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next,
                        ContinueWithFunc = x => x?.Operand?.ToString() == "System.Void System.Exception::.ctor(System.String)"
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Format",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = ".ctor",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next,
                        ContinueWithFunc = x => x?.Operand?.ToString() == "System.Void System.Security.SecurityException::.ctor(System.String)"
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Get_Response",
                    Steps = 1,
                    Direction = Extensions.InstructionExtensions.Direction.Previous,
                    ContinueWithFunc = x => x?.Operand?.ToString()?.StartsWith("System.Web.HttpResponse") == true
                },
            };
            patterns.ForEach(p => p.SearchType = SanitizerSearchPattern.Type.Format);
            return patterns;
        }
        private static IEnumerable<SanitizerSearchPattern> CreateConcatSearchPatterns()
        {
            var patterns = new List<SanitizerSearchPattern>
            {
                new SanitizerSearchPattern()
                {
                    Pattern = "Concat",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWithFunc = x => x.OpCode == OpCodes.Nop || (x.Operand as MethodReference) == null
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Concat",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = "Parameter",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Concat",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = "Log",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Log",
                    Steps = 1,
                    Direction = Extensions.InstructionExtensions.Direction.Previous
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Get_Item",
                    Steps = 1,
                    Direction = Extensions.InstructionExtensions.Direction.Next
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Concat",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = ".ctor",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next,
                        ContinueWithFunc = x => x?.Operand?.ToString() == "System.Void System.Exception::.ctor(System.String)"
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Concat",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = ".ctor",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next,
                        ContinueWithFunc = x => x?.Operand?.ToString() == "System.Void System.Security.SecurityException::.ctor(System.String)"
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Get_Response",
                    Steps = 1,
                    Direction = Extensions.InstructionExtensions.Direction.Previous,
                    ContinueWithFunc = x => x?.Operand?.ToString()?.StartsWith("System.Web.HttpResponse") == true
                },
            };
            patterns.ForEach(p => p.SearchType = SanitizerSearchPattern.Type.Concat);
            return patterns;
        }
        private static IEnumerable<SanitizerSearchPattern> CreateAppendSearchPatterns()
        {
            var patterns = new List<SanitizerSearchPattern>
            {
                new SanitizerSearchPattern()
                {
                    Pattern = "Append",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWithFunc = x => x.OpCode == OpCodes.Nop || (x.Operand as MethodReference) == null
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Append",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = "Parameter",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Append",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = "Log",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Log",
                    Steps = 1,
                    Direction = Extensions.InstructionExtensions.Direction.Previous
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Get_Item",
                    Steps = 1,
                    Direction = Extensions.InstructionExtensions.Direction.Next
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Append",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = ".ctor",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next,
                        ContinueWithFunc = x => x?.Operand?.ToString() == "System.Void System.Exception::.ctor(System.String)"
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Append",
                    Steps = 10,
                    Direction = Extensions.InstructionExtensions.Direction.Next,
                    ContinueWith = new SanitizerSearchPattern()
                    {
                        Pattern = ".ctor",
                        Steps = 5,
                        Direction = Extensions.InstructionExtensions.Direction.Next,
                        ContinueWithFunc = x => x?.Operand?.ToString() == "System.Void System.Security.SecurityException::.ctor(System.String)"
                    }
                },
                new SanitizerSearchPattern()
                {
                    Pattern = "Get_Response",
                    Steps = 1,
                    Direction = Extensions.InstructionExtensions.Direction.Previous,
                    ContinueWithFunc = x => x?.Operand?.ToString()?.StartsWith("System.Web.HttpResponse") == true
                },
            };
            patterns.ForEach(p => p.SearchType = SanitizerSearchPattern.Type.Append);
            return patterns;
        }
    }
}
