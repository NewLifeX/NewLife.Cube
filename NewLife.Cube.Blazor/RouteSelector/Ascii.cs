
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal static class Ascii
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AsciiIgnoreCaseEquals(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int length)
        {
            if (a.Length < length || b.Length < length)
            {
                ThrowArgumentExceptionForLength();
            }

            ref var charA = ref MemoryMarshal.GetReference(a);
            ref var charB = ref MemoryMarshal.GetReference(b);

            while (length > 0 && AsciiIgnoreCaseEquals(charA, charB))
            {
                charA = ref Unsafe.Add(ref charA, 1);
                charB = ref Unsafe.Add(ref charB, 1);
                length--;
            }

            return length == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AsciiIgnoreCaseEquals(char charA, char charB)
        {
            const uint AsciiToLower = 0x20;
            return
                charA == charB ||

                ((charA | AsciiToLower) == (charB | AsciiToLower) && (uint)((charA | AsciiToLower) - 'a') <= (uint)('z' - 'a'));
        }

        public static bool IsAscii(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] > (char)0x7F)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ThrowArgumentExceptionForLength()
        {
            throw new ArgumentException("length");
        }
    }
}
