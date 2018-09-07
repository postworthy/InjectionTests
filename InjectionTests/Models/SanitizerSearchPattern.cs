using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static InjectionTests.Extensions.InstructionExtensions;

namespace InjectionTests.Models
{
    public class SanitizerSearchPattern
    {
        public enum Type : uint
        {
            All = 0,
            Format,
            Concat,
            Append
        }
        public SanitizerSearchPattern.Type SearchType { get; set; } = SanitizerSearchPattern.Type.All;
        public string Pattern { get; set; } = null;
        public Direction Direction { get; set; } = Direction.Both;
        public uint Steps { get; set; } = 1;
        public SanitizerSearchPattern ContinueWith { get; set; } = null;
        public Func<Instruction, bool> ContinueWithFunc { get; set; } = null;
        public bool IsMatch(Instruction instruction)
        {
            if (string.IsNullOrEmpty(Pattern))
                return false;

            if (Pattern == ".") //Self, or same node
            {
                if (ContinueWith != null)
                    return ContinueWith.IsMatch(instruction);
                else if (ContinueWithFunc != null)
                    return ContinueWithFunc(instruction);
                else
                    return false;
            }

            if (ContinueWith != null)
            {
                var result = instruction.Nearest(Pattern, Direction, Steps);
                return ContinueWith.IsMatch(result);
            }
            else if(ContinueWithFunc != null)
            {
                var result = instruction.Nearest(Pattern, Direction, Steps);
                return ContinueWithFunc(result);
            }
            else
                return instruction.IsNearMethod(Pattern, Direction, Steps);
        }
        public bool IsNotMatch(Instruction instruction)
        {
            return !IsMatch(instruction);
        }
    }
}
