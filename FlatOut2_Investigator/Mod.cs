using FlatOut2.SDK;
using FlatOut2.SDK.API;
using FlatOut2.SDK.Structs;
using FlatOut2_Investigator.Configuration;
using FlatOut2_Investigator.Template;
using Microsoft.VisualBasic;
using Reloaded.Hooks.Definitions.Structs;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;

namespace FlatOut2_Investigator
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private static int GetNumOptional(string str, string prefix, int def, int offset)
        {
            if (str.Length > prefix.Length)
                return Convert.ToInt32(str[prefix.Length..]) + offset;

            return def;
        }

        private unsafe void* GetPointerFromName(string name)
        {
            if (name.StartsWith("player"))
                return (void*)Info.Race.GetPlayers()[GetNumOptional(name, "player", 0, -1)];

            if (name.StartsWith("car"))
                return ((Player*)Info.Race.GetPlayers()[GetNumOptional(name, "car", 0, -1)])->Car;

            return (name) switch
            {
                "garage" => &(*RaceInfo.Instance)->PlayerProfile.Garage,
                "profile" => &(*RaceInfo.Instance)->PlayerProfile,
                "host" => (*RaceInfo.Instance)->HostObject,
                "menu" => (*RaceInfo.Instance)->MenuInterface,
                _ => *RaceInfo.Instance
            };
        }

        private string PrintBuffer = "";

        private unsafe void ReadStringFull(string full)
        {
            PrintBuffer = "";

            foreach (var i in full.Split("\n"))
                ReadString(i);

            _logger.WriteLine(PrintBuffer);
        }

        private static unsafe void SetDynamically<T>(nint ptr, dynamic val, T refVal)
        {
            *(T*)ptr = (T)val;
        }

        private unsafe dynamic StringToDynamic(string type, string value)
        {
            int numBase = (type.StartsWith("hex") || type.StartsWith("mask")) ? 16 : 10;

            return type switch
            {
                "ansi" => Marshal.StringToHGlobalAnsi(value),
                "uni" => Marshal.StringToHGlobalUni(value),
                "float" => Convert.ToSingle(value),
                "double" => Convert.ToDouble(value),

                "long" or
                "int64" or
                "mask64" or
                "hex64" => Convert.ToInt64(value, numBase),

                "uint" or
                "hex32" or
                "hex" or
                "mask32" or
                "mask" => Convert.ToUInt32(value, numBase),

                "short" or
                "int16" => Convert.ToInt16(value, numBase),

                "ushort" or
                "hex16" or
                "mask16" => Convert.ToUInt16(value, numBase),

                "byte" or
                "hex8" or
                "mask8" => Convert.ToByte(value, numBase),

                "sbyte" or
                "int8" => Convert.ToSByte(value, numBase),

                "int" or
                "int32" or
                _ => Convert.ToInt32(value, numBase),
            };
        }

        private static unsafe dynamic GetDynamically(string type, nint ptr)
        {
            return type switch
            {
                "ansi" => Marshal.PtrToStringAnsi(*(nint*)ptr)!,
                "uni" => Marshal.PtrToStringUni(*(nint*)ptr)!,

                "float" => *(float*)ptr,
                "double" => *(double*)ptr,

                "long" or
                "int64" or
                "mask64" or
                "hex64" => *(long*)ptr,

                "uint" or
                "mask32" or
                "mask" or
                "hex32" or
                "hex" => *(uint*)ptr,

                "short" or
                "int16" => *(short*)ptr,

                "ushort" or
                "mask16" or
                "hex16" => *(ushort*)ptr,

                "byte" or
                "mask8" or
                "hex8" => *(byte*)ptr,

                "sbyte" or
                "int8" => *(sbyte*)ptr,

                "int" or
                "int32" or
                _ => *(int*)ptr,
            };
        }

        private unsafe void ReadString(string input)
        {
            var split = input.Split('=');
            var leftSide = split[0].Trim().Split(" ");

            nint addr = Convert.ToInt32(leftSide[^1].Trim(), 16);

            if (leftSide[0] != "raw")
                addr += (nint)GetPointerFromName(leftSide[^3]);

            string type = leftSide[^2].Trim();
            dynamic val = GetDynamically(type, addr);

            if (type.StartsWith("hex"))
            {
                val = Convert.ToString(val, 16);
                val = val.PadLeft(GetNumOptional(type, "hex", 32, 0) / 4, '0');
            }

            if (type.StartsWith("mask"))
            {
                val = Convert.ToString(val, 2);
                val = val.PadLeft(GetNumOptional(type, "mask", 32, 0), '0');
            }

            PrintBuffer += $"{val}, ";

            if (input.Contains('='))
                SetDynamically(addr, StringToDynamic(type, split[1].Trim()), val);
        }

        private static string CodeFilename = "that.txt";
        private static string CurrentCode = File.ReadAllText(CodeFilename);
        DateTime CodeTime = FileSystem.FileDateTime(CodeFilename);

        void PerFrame()
        {
            var current = FileSystem.FileDateTime(CodeFilename);
            if (current != CodeTime)
            {
                CurrentCode = File.ReadAllText(CodeFilename);
                CodeTime = current;
            }

            ReadStringFull(CurrentCode);
        }

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            SDK.Init(_hooks!);
            Helpers.HookPerFrame(PerFrame);
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}