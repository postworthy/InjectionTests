using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InjectionTests.Extensions.Extensions
{
    public static class InstructionExtensions
    {
        public enum Direction
        {
            Next,
            Previous,
            Both
        }
        public static bool IsNearMethod(this Instruction inst, string name, Direction direction = Direction.Both, uint steps = 1)
        {
            if (inst == null) return false;

            if (direction == Direction.Next || direction == Direction.Both)
            {
                var x = inst;
                for (int i = 0; i < steps && x != null; i++)
                {
                    if ((x?.Next?.Operand as MethodReference)?.FullName?.ToLower()?.Contains(name.ToLower()) == true)
                        return true;

                    x = x?.Next;
                }
            }
            if (direction == Direction.Previous || direction == Direction.Both)
            {
                var x = inst;
                for (int i = 0; i < steps && x != null; i++)
                {
                    if ((x?.Previous?.Operand as MethodReference)?.FullName?.ToLower()?.Contains(name.ToLower()) == true)
                        return true;

                    x = x?.Previous;
                }
            }
            return false;
        }

        public static Instruction Nearest(this Instruction inst, string name, Direction direction = Direction.Both, uint steps = 1)
        {
            if (inst == null) return Instruction.Create(OpCodes.Nop);

            if (direction == Direction.Next || direction == Direction.Both)
            {
                var x = inst;
                for (int i = 0; i < steps && x != null; i++)
                {
                    if ((x?.Next?.Operand as MethodReference)?.FullName?.ToLower()?.Contains(name.ToLower()) == true)
                        return x.Next;

                    x = x?.Next;
                }
            }
            if (direction == Direction.Previous || direction == Direction.Both)
            {
                var x = inst;
                for (int i = 0; i < steps && x != null; i++)
                {
                    if ((x?.Previous?.Operand as MethodReference)?.FullName?.ToLower()?.Contains(name.ToLower()) == true)
                        return x.Previous;

                    x = x?.Previous;
                }
            }
            return Instruction.Create(OpCodes.Nop);
        }

        public static Instruction Nearest(this Instruction inst, OpCode opcode, Direction direction = Direction.Both, uint steps = 1)
        {
            if (inst == null) return Instruction.Create(OpCodes.Nop);

            if (direction == Direction.Next || direction == Direction.Both)
            {
                var x = inst;
                for (int i = 0; i < steps && x != null; i++)
                {
                    if (x?.Next?.OpCode == opcode)
                        return x.Next;

                    x = x?.Next;
                }
            }
            if (direction == Direction.Previous || direction == Direction.Both)
            {
                var x = inst;
                for (int i = 0; i < steps && x != null; i++)
                {
                    if (x?.Previous?.OpCode == opcode)
                        return x.Previous;

                    x = x?.Previous;
                }
            }
            return Instruction.Create(OpCodes.Nop);
        }
        public static IEnumerable<Instruction> Take(this Instruction inst, int count, Direction direction = Direction.Both)
        {
            if (inst == null) yield break;

            if (direction == Direction.Next || direction == Direction.Both)
            {
                var x = inst;
                for (int i = 0; i < count && x != null; i++)
                {
                    yield return x.Next;
                    x = x.Next;
                }
            }

            if (direction == Direction.Previous || direction == Direction.Both)
            {
                var x = inst;
                for (int i = 0; i < count && x != null; i++)
                {
                    yield return x.Previous;
                    x = x.Previous;
                }
            }
        }
    }
}
