﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MapChooserExtended
{
    public class ForceFullUpdate
    {
        private delegate IntPtr GetAddonNameDelegate(IntPtr thisPtr);
        private readonly ForceFullUpdate.INetworkServerService networkServerService = new();

        [StructLayout(LayoutKind.Sequential)]
        struct CUtlMemory
        {
            public unsafe nint* m_pMemory;
            public int m_nAllocationCount;
            public int m_nGrowSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CUtlVector
        {
            public unsafe nint this[int index]
            {
                get => this.m_Memory.m_pMemory[index];
                set => this.m_Memory.m_pMemory[index] = value;
            }

            public int m_iSize;
            public CUtlMemory m_Memory;

            public nint Element(int index) => this[index];
        }

        public class INetworkServerService : NativeObject
        {
            private readonly VirtualFunctionWithReturn<nint, nint> GetIGameServerFunc;

            public INetworkServerService() : base(NativeAPI.GetValveInterface(0, "NetworkServerService_001"))
            {
                this.GetIGameServerFunc = new VirtualFunctionWithReturn<nint, nint>(this.Handle, GameData.GetOffset("INetworkServerService_GetIGameServer"));
            }

            public INetworkGameServer GetIGameServer()
            {
                return new INetworkGameServer(this.GetIGameServerFunc.Invoke(this.Handle));
            }
        }

        public class INetworkGameServer : NativeObject
        {
            private static int SlotsOffset = GameData.GetOffset("INetworkGameServer_Slots");

            private CUtlVector Slots;

            public INetworkGameServer(nint ptr) : base(ptr)
            {
                this.Slots = Marshal.PtrToStructure<CUtlVector>(base.Handle + SlotsOffset);
            }
        }

        public string GetAddonID()
        {
            IntPtr networkGameServer = networkServerService.GetIGameServer().Handle;
            IntPtr vtablePtr = Marshal.ReadIntPtr(networkGameServer);
            IntPtr functionPtr = Marshal.ReadIntPtr(vtablePtr + (25 * IntPtr.Size));
            var getAddonName = Marshal.GetDelegateForFunctionPointer<GetAddonNameDelegate>(functionPtr);
            IntPtr result = getAddonName(networkGameServer);
            return Marshal.PtrToStringAnsi(result)!.Split(',')[0];
        }
    }
}
