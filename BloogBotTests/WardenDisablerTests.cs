using BloogBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;

namespace BloogBotTests
{
    [TestClass]
    public class WardenDisablerTests
    {
        const uint MEM_COMMIT = 0x1000;
        const uint MEM_RESERVE = 0x2000;
        const uint MEM_RELEASE = 0x8000;
        const uint PAGE_NOACCESS = 0x01;
        const uint PAGE_READWRITE = 0x04;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAlloc(IntPtr address, UIntPtr size, uint allocationType, uint protect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualFree(IntPtr address, UIntPtr size, uint freeType);

        [TestMethod]
        public void RequireValidAssemblyPropagatesAssemblerFailure()
        {
            var expected = new InvalidOperationException("assembly failed");
            var thrown = Assert.ThrowsException<InvalidOperationException>(() =>
                MemoryManager.RequireValidAssembly(() => throw expected, null));

            Assert.AreSame(expected, thrown);
        }

        [TestMethod]
        public void RequireValidAssemblyRejectsEmptyMachineCode()
        {
            var thrown = Assert.ThrowsException<InvalidOperationException>(() =>
                MemoryManager.RequireValidAssembly(() => new byte[0], null));

            StringAssert.Contains(thrown.Message, "no machine code");
        }

        [TestMethod]
        public void RequireValidAssemblyRejectsMachineCodeLargerThanAllocation()
        {
            var thrown = Assert.ThrowsException<InvalidOperationException>(() =>
                MemoryManager.RequireValidAssembly(() => new byte[5], 4));

            StringAssert.Contains(thrown.Message, "exceeds");
        }

        [TestMethod]
        public void FindPatternInReadableMemorySkipsInaccessibleAllocationBoundary()
        {
            var pageSize = Environment.SystemPageSize;
            var allocation = VirtualAlloc(IntPtr.Zero, (UIntPtr)(pageSize * 2), MEM_RESERVE, PAGE_NOACCESS);
            Assert.AreNotEqual(IntPtr.Zero, allocation);

            try
            {
                var readablePage = VirtualAlloc(allocation, (UIntPtr)pageSize, MEM_COMMIT, PAGE_READWRITE);
                Assert.AreEqual(allocation, readablePage);

                var signature = new byte[] { 0x8B, 0x45, 0x08, 0x8A, 0x04 };
                var expected = IntPtr.Add(readablePage, pageSize - signature.Length);
                Marshal.Copy(signature, 0, expected, signature.Length);

                var actual = MemoryManager.FindPatternInReadableMemory(
                    allocation,
                    pageSize * 2,
                    signature);

                Assert.AreEqual(expected, actual);
            }
            finally
            {
                Assert.IsTrue(VirtualFree(allocation, UIntPtr.Zero, MEM_RELEASE));
            }
        }
    }
}
