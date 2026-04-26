using Keen.VRage.Library.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ClientPlugin.Helpers;

/// <summary>
/// Reads head pose data from OpenTrack via the FreeTrack shared memory protocol
/// OpenTrack must be running and configured to output via "Freetrack 2.0 Enhanced" or "OpenTrack"
/// </summary>
public static class OpenTrackReader
{
    #region Fields

    // Standard FreeTrack/OpenTrack shared memory map name
    //private const string mapName = "FTNoIR_Mem_Map";
    private const string mapName = "FT_SharedMem";

    private static MemoryMappedViewAccessor? _accessor;
    private static MemoryMappedFile? _mmf;
    private static bool loggedError = false;
    private static bool loggedStartup = false;

    #endregion Fields

    #region Properties

    private static float multiplier => Config.Current.Multiplier;
    private static bool trackingEnabled => Config.Current.Enabled;

    #endregion Properties

    #region Methods

    /// <summary>
    /// Attempts to read the current head pose from OpenTrack shared memory
    /// Returns false if OpenTrack is not running, or the map is unavailable
    /// </summary>
    public static bool TryGetPose(out float yaw, out float pitch)
    {
        yaw = pitch = 0f;

        if (trackingEnabled)
        {
            try
            {
                _mmf ??= MemoryMappedFile.OpenExisting(mapName);

                if (!loggedStartup)
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] Found shared memory map: '{mapName}'");
                    loggedStartup = true;
                }

                _accessor ??= _mmf.CreateViewAccessor(0, Marshal.SizeOf<FTSharedMemData>(), MemoryMappedFileAccess.Read);

                _accessor.Read(0, out FTSharedMemData data);

                yaw = data.Yaw * multiplier;
                pitch = data.Pitch * multiplier;
                return true;
            }
            catch
            {
                // OpenTrack not running — Log once to avoid filling the log with error messages
                if (!loggedError)
                {
                    Log.Default.WriteLine($"[{Plugin.Name}] OpenTrack shared memory not found.  " +
                        "Ensure OpenTrack is running with 'FreeTrack 2.0 Enhanced' output selected.");
                    loggedError = true;
                }

                try { _accessor?.Dispose(); } catch { }
                try { _mmf?.Dispose(); } catch { }
                _accessor = null;
                _mmf = null;
                return false;
            }
        }
        return true;
    }

    #endregion Methods
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FTSharedMemData
{
    // Incremented each update — can be used for change detection
    public uint DataID;

    public int CamWidth;
    public int CamHeight;

    public float Yaw;
    public float Pitch;
    public float Roll;
    public float X;
    public float Y;
    public float Z;
}

//[StructLayout(LayoutKind.Sequential, Pack = 4)]
//public struct OpenTrackData
//{
//    // Incremented each update — can be used for change detection
//    public int DataID;

//    public int CamWidth;
//    public int CamHeight;

//    public float X;
//    public float Y;
//    public float Z;
//    public float Yaw;
//    public float Pitch;
//    public float Roll;
//}